using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SteamControllerBridge.Bridge;

internal sealed class LocalControlHttpServer : IDisposable
{
    public const int Port = 47309;
    public const string TransportName = "SteamControllerBridge.ControlHttp.v1";

    private readonly BridgeService _bridge;
    private readonly Action<string> _log;
    private CancellationTokenSource? _cts;
    private TcpListener? _listener;
    private Task? _serverTask;

    public LocalControlHttpServer(BridgeService bridge, Action<string> log)
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
        _listener = new TcpListener(IPAddress.Loopback, Port);
        _listener.Start();
        _serverTask = Task.Run(() => RunAsync(_cts.Token));
        _log($"Local control HTTP listening: http://127.0.0.1:{Port}/");
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is not null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Local control HTTP error: {ex.Message}");
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var request = await ReadHttpRequestBodyAsync(stream, token).ConfigureAwait(false);
            var responseJson = LocalControlServer.HandleRequest(_bridge, request, TransportName);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            var header =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                $"Content-Length: {responseBytes.Length}\r\n" +
                "Connection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, token).ConfigureAwait(false);
            await stream.WriteAsync(responseBytes, token).ConfigureAwait(false);
        }
    }

    private static async Task<string?> ReadHttpRequestBodyAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = new byte[16 * 1024];
        var bytes = 0;
        while (bytes < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(bytes, buffer.Length - bytes), token).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            bytes += read;
            if (FindHeaderEnd(buffer, bytes) >= 0)
            {
                break;
            }
        }

        if (bytes == 0)
        {
            return null;
        }

        var headerEnd = FindHeaderEnd(buffer, bytes);
        if (headerEnd < 0)
        {
            return null;
        }

        var headerText = Encoding.ASCII.GetString(buffer, 0, headerEnd);
        var contentLength = GetContentLength(headerText);
        var bodyStart = headerEnd + 4;
        var bodyBytes = bytes - bodyStart;

        if (contentLength > buffer.Length - bodyStart)
        {
            throw new InvalidOperationException("Local control HTTP request is too large.");
        }

        while (bodyBytes < contentLength)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(bodyStart + bodyBytes, contentLength - bodyBytes), token).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            bodyBytes += read;
        }

        return Encoding.UTF8.GetString(buffer, bodyStart, Math.Min(bodyBytes, contentLength));
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (var i = 0; i <= length - 4; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetContentLength(string headers)
    {
        foreach (var line in headers.Split(["\r\n"], StringSplitOptions.None))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            if (line[..separator].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line[(separator + 1)..].Trim(), out var length))
            {
                return Math.Max(0, length);
            }
        }

        return 0;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
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
