using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreSpotUWPLoginHelper.Services;

internal sealed class LoopbackCallbackListener : IAsyncDisposable
{
    private readonly HttpListener _listener = new();

    public LoopbackCallbackListener(string prefix)
    {
        _listener.Prefixes.Add(prefix.EndsWith("/") ? prefix : $"{prefix}/");
    }

    public void Start() => _listener.Start();

    public async Task<Uri> WaitForCallbackAsync(CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                _listener.Stop();
            }
            catch
            {
            }
        });

        HttpListenerContext context;
        try
        {
            context = await _listener.GetContextAsync();
        }
        catch (HttpListenerException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The browser sign-in wait was cancelled.", ex, cancellationToken);
        }

        await WriteResponseAsync(context.Response);
        return context.Request.Url ?? throw new InvalidOperationException("The browser callback URL was missing.");
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response)
    {
        const string body = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <title>LibreSpotUWP Login Helper</title>
                <style>
                    body { font-family: "Segoe UI Variable Text", "Segoe UI", sans-serif; background: #111827; color: #f9fafb; margin: 0; padding: 32px; }
                    main { max-width: 640px; margin: 64px auto; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.12); border-radius: 28px; padding: 32px; }
                    h1 { margin-top: 0; font-size: 32px; }
                    p { font-size: 16px; line-height: 1.6; color: #d1d5db; }
                </style>
            </head>
            <body>
                <main>
                    <h1>Sign-in captured</h1>
                    <p>You can return to the LibreSpotUWP Login Helper now.</p>
                </main>
            </body>
            </html>
            """;

        var bytes = Encoding.UTF8.GetBytes(body);
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(bytes);
    }

    public ValueTask DisposeAsync()
    {
        _listener.Close();
        return ValueTask.CompletedTask;
    }
}
