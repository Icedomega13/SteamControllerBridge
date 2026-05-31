using System.IO.Pipes;
using System.Text.Json;

namespace SteamControllerBridge.Bridge;

internal sealed class LocalControlServer : IDisposable
{
    public const string PipeName = "SteamControllerBridge.Control.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly BridgeService _bridge;
    private readonly Action<string> _log;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public LocalControlServer(BridgeService bridge, Action<string> log)
    {
        _bridge = bridge;
        _log = log;
    }

    public void Start()
    {
        if (_serverTask is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunAsync(_cts.Token));
        _log($"Local control pipe listening: {PipeName}");
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                await HandleClientAsync(pipe, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Local control pipe error: {ex.Message}");
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken token)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };

        var request = await reader.ReadLineAsync(token).ConfigureAwait(false);
        var response = HandleRequest(request);
        await writer.WriteLineAsync(response).ConfigureAwait(false);
    }

    internal static string HandleRequest(BridgeService bridge, string? request, string transportName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return SerializeError(transportName, "Empty local control request.");
            }

            using var doc = JsonDocument.Parse(request);
            var root = doc.RootElement;
            var action = GetString(root, "action");
            var snapshot = action.Trim().ToLowerInvariant() switch
            {
                "status" or "ping" => bridge.GetLocalControlSnapshot(),
                "start" or "on" => StartBridge(bridge),
                "stop" or "off" => StopBridge(bridge),
                "toggle" => ToggleBridge(bridge),
                "setrumbleintensity" => bridge.SetLocalRumbleIntensity(GetInt32(root, "value")),
                "setpaddle" => bridge.SetLocalPaddleMapping(GetString(root, "paddle"), GetString(root, "output")),
                _ => throw new ArgumentException($"Unknown local control action '{action}'.")
            };

            return JsonSerializer.Serialize(new
            {
                ok = true,
                pipe = transportName,
                status = snapshot
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return SerializeError(transportName, ex.Message);
        }
    }

    private string HandleRequest(string? request)
    {
        return HandleRequest(_bridge, request, PipeName);
    }

    private static LocalControlSnapshot StartBridge(BridgeService bridge)
    {
        bridge.Start();
        return bridge.GetLocalControlSnapshot();
    }

    private static LocalControlSnapshot StopBridge(BridgeService bridge)
    {
        bridge.Stop();
        return bridge.GetLocalControlSnapshot();
    }

    private static LocalControlSnapshot ToggleBridge(BridgeService bridge)
    {
        if (bridge.Status.IsEnabled || bridge.Status.IsWorking)
        {
            bridge.Stop();
        }
        else
        {
            bridge.Start();
        }

        return bridge.GetLocalControlSnapshot();
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Missing string property '{propertyName}'.");
        }

        return value.GetString() ?? string.Empty;
    }

    private static int GetInt32(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || !value.TryGetInt32(out var result))
        {
            throw new ArgumentException($"Missing integer property '{propertyName}'.");
        }

        return result;
    }

    private static string SerializeError(string transportName, string message)
    {
        return JsonSerializer.Serialize(new
        {
            ok = false,
            pipe = transportName,
            error = message
        }, JsonOptions);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _serverTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Shutdown best effort only.
        }

        _cts?.Dispose();
    }
}
