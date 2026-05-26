using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace SteamControllerBridge.Bridge;

internal sealed class DsuMotionServer : IDisposable
{
    private const int Port = 26760;
    private const ushort ProtocolVersion = 1001;
    private const uint ServerId = 0x53434231; // SCB1
    private const uint ClientMagic = 0x43555344; // DSUC
    private const uint ServerMagic = 0x53555344; // DSUS
    private const uint MessageProtocolVersion = 0x100000;
    private const uint MessageControllerInfo = 0x100001;
    private const uint MessageControllerData = 0x100002;

    private static readonly byte[] ControllerMac = { 0x28, 0xDE, 0x13, 0x04, 0x00, 0x01 };

    private readonly object _gate = new();
    private readonly Action<string> _log;
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private IPEndPoint? _subscribedClient;
    private DateTime _subscriptionExpiresAtUtc = DateTime.MinValue;
    private MotionState _motion;
    private uint _packetCounter;
    private bool _loggedClient;

    public DsuMotionServer(Action<string> log)
    {
        _log = log;
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _udp is not null;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    public void Update(SteamControllerInput input)
    {
        if (!input.HasGyro)
        {
            return;
        }

        lock (_gate)
        {
            _motion = new MotionState(
                Connected: true,
                Timestamp: (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000UL,
                AccelX: input.AccelX / 4096f,
                AccelY: input.AccelY / 4096f,
                AccelZ: input.AccelZ / 4096f,
                GyroPitch: input.GyroX / 16.4f,
                GyroYaw: input.GyroY / 16.4f,
                GyroRoll: input.GyroZ / 16.4f);
        }
    }

    public void SetControllerConnected(bool connected)
    {
        lock (_gate)
        {
            _motion = _motion with { Connected = connected };
        }
    }

    private void Start()
    {
        lock (_gate)
        {
            if (_udp is not null)
            {
                return;
            }

            try
            {
                _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, Port));
                _cts = new CancellationTokenSource();
                _loggedClient = false;
                _loopTask = Task.Run(() => RunAsync(_udp, _cts.Token));
                _ = Task.Run(() => StreamAsync(_udp, _cts.Token));
                _log("DSU motion server listening on 127.0.0.1:26760.");
                _log("Emulator setup: add a DSU/Cemuhook UDP motion source at IP 127.0.0.1, port 26760.");
            }
            catch (SocketException ex)
            {
                _udp?.Dispose();
                _udp = null;
                _cts?.Dispose();
                _cts = null;
                _log($"DSU motion server could not start on 127.0.0.1:{Port}: {ex.Message}");
            }
        }
    }

    private void Stop()
    {
        UdpClient? udp;
        CancellationTokenSource? cts;
        Task? loopTask;
        lock (_gate)
        {
            udp = _udp;
            cts = _cts;
            loopTask = _loopTask;
            _udp = null;
            _cts = null;
            _loopTask = null;
            _subscribedClient = null;
            _subscriptionExpiresAtUtc = DateTime.MinValue;
            _loggedClient = false;
        }

        cts?.Cancel();
        udp?.Dispose();
        try
        {
            loopTask?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // Best-effort shutdown.
        }

        cts?.Dispose();
        if (udp is not null)
        {
            _log("DSU motion server stopped.");
        }
    }

    private async Task RunAsync(UdpClient udp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try
            {
                received = await udp.ReceiveAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"DSU motion server stopped reading: {ex.Message}");
                break;
            }

            var responses = BuildResponses(received.Buffer, received.RemoteEndPoint);
            foreach (var response in responses)
            {
                try
                {
                    await udp.SendAsync(response, received.RemoteEndPoint, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log($"DSU motion server send failed: {ex.Message}");
                }
            }
        }
    }

    private async Task StreamAsync(UdpClient udp, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(8));
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            IPEndPoint? client;
            lock (_gate)
            {
                client = DateTime.UtcNow <= _subscriptionExpiresAtUtc ? _subscribedClient : null;
            }

            if (client is null)
            {
                continue;
            }

            try
            {
                var packet = BuildControllerDataResponse();
                await udp.SendAsync(packet, client, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"DSU motion stream failed: {ex.Message}");
            }
        }
    }

    private IReadOnlyList<byte[]> BuildResponses(byte[] request, IPEndPoint remoteEndPoint)
    {
        if (request.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(request.AsSpan(0, 4)) != ClientMagic)
        {
            return Array.Empty<byte[]>();
        }

        var messageType = BinaryPrimitives.ReadUInt32LittleEndian(request.AsSpan(16, 4));
        if (!_loggedClient)
        {
            _loggedClient = true;
            _log($"DSU motion client connected from {remoteEndPoint.Address}:{remoteEndPoint.Port}.");
        }

        return messageType switch
        {
            MessageProtocolVersion => new[] { BuildProtocolVersionResponse() },
            MessageControllerInfo => BuildControllerInfoResponses(request),
            MessageControllerData => HandleControllerDataRequest(request, remoteEndPoint),
            _ => Array.Empty<byte[]>()
        };
    }

    private static byte[] BuildProtocolVersionResponse()
    {
        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload);
        writer.Write(MessageProtocolVersion);
        writer.Write((ushort)ProtocolVersion);
        return WrapPayload(payload.ToArray());
    }

    private static IReadOnlyList<byte[]> BuildControllerInfoResponses(byte[] request)
    {
        if (request.Length < 25)
        {
            return new[] { BuildControllerInfoResponse(0) };
        }

        var count = Math.Clamp(BinaryPrimitives.ReadInt32LittleEndian(request.AsSpan(20, 4)), 1, 4);
        var responses = new List<byte[]>(count);
        for (var index = 0; index < count && 24 + index < request.Length; index++)
        {
            var slot = request[24 + index];
            responses.Add(BuildControllerInfoResponse(slot));
        }

        return responses.Count == 0 ? new[] { BuildControllerInfoResponse(0) } : responses;
    }

    private static byte[] BuildControllerInfoResponse(byte slot)
    {
        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload);
        writer.Write(MessageControllerInfo);
        WriteControllerInfo(writer, slot, connected: slot == 0);
        writer.Write((byte)0);
        return WrapPayload(payload.ToArray());
    }

    private IReadOnlyList<byte[]> HandleControllerDataRequest(byte[] request, IPEndPoint remoteEndPoint)
    {
        var wantsAll = request.Length <= 20 || request[20] == 0;
        var wantsSlotZero = request.Length > 21 && (request[20] & 1) == 1 && request[21] == 0;
        var wantsMac = request.Length >= 28 && (request[20] & 2) == 2 && request.AsSpan(22, 6).SequenceEqual(ControllerMac);
        if (!wantsAll && !wantsSlotZero && !wantsMac)
        {
            return Array.Empty<byte[]>();
        }

        lock (_gate)
        {
            _subscribedClient = remoteEndPoint;
            _subscriptionExpiresAtUtc = DateTime.UtcNow.AddSeconds(5);
        }

        return new[] { BuildControllerDataResponse() };
    }

    private byte[] BuildControllerDataResponse()
    {
        MotionState motion;
        uint packet;
        lock (_gate)
        {
            motion = _motion;
            packet = ++_packetCounter;
        }

        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload);
        writer.Write(MessageControllerData);
        WriteControllerInfo(writer, 0, connected: motion.Connected);
        writer.Write((byte)(motion.Connected ? 1 : 0));
        writer.Write(packet);
        writer.Write((byte)0); // Buttons cluster 1
        writer.Write((byte)0); // Buttons cluster 2
        writer.Write((byte)0); // Home
        writer.Write((byte)0); // Touch
        writer.Write((byte)128); // Left stick X
        writer.Write((byte)128); // Left stick Y
        writer.Write((byte)128); // Right stick X
        writer.Write((byte)128); // Right stick Y
        writer.Write((byte)0); // D-pad left analog
        writer.Write((byte)0); // D-pad down analog
        writer.Write((byte)0); // D-pad right analog
        writer.Write((byte)0); // D-pad up analog
        writer.Write((byte)0); // Square/X analog
        writer.Write((byte)0); // Cross/A analog
        writer.Write((byte)0); // Circle/B analog
        writer.Write((byte)0); // Triangle/Y analog
        writer.Write((byte)0); // R1 analog
        writer.Write((byte)0); // L1 analog
        writer.Write((byte)0); // R2 analog
        writer.Write((byte)0); // L2 analog
        WriteInactiveTouch(writer, id: 0);
        WriteInactiveTouch(writer, id: 1);
        writer.Write(motion.Timestamp);
        writer.Write(motion.AccelX);
        writer.Write(motion.AccelY);
        writer.Write(motion.AccelZ);
        writer.Write(motion.GyroPitch);
        writer.Write(motion.GyroYaw);
        writer.Write(motion.GyroRoll);
        return WrapPayload(payload.ToArray());
    }

    private static void WriteControllerInfo(BinaryWriter writer, byte slot, bool connected)
    {
        writer.Write(slot);
        writer.Write((byte)(connected ? 2 : 0));
        writer.Write((byte)(connected ? 2 : 0));
        writer.Write((byte)(connected ? 1 : 0));
        writer.Write(connected ? ControllerMac : new byte[6]);
        writer.Write((byte)(connected ? 5 : 0));
    }

    private static void WriteInactiveTouch(BinaryWriter writer, byte id)
    {
        writer.Write((byte)0);
        writer.Write(id);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
    }

    private static byte[] WrapPayload(byte[] payload)
    {
        var packet = new byte[16 + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(0, 4), ServerMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), ProtocolVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6, 2), (ushort)payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12, 4), ServerId);
        payload.CopyTo(packet.AsSpan(16));
        var crc = Crc32(packet);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8, 4), crc);
        return packet;
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    public void Dispose()
    {
        Stop();
    }

    private readonly record struct MotionState(
        bool Connected,
        ulong Timestamp,
        float AccelX,
        float AccelY,
        float AccelZ,
        float GyroPitch,
        float GyroYaw,
        float GyroRoll);
}
