using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreSpotUWPLoginHelper.Models;

namespace LibreSpotUWPLoginHelper.Services;

internal sealed class SpotifyAuthBroker
{
    private const int LoopbackPort = 8898;
    private SpotifyPkceRequest? _pendingRequest;

    public SpotifyPkceRequest CreateRequest(SpotifyAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("The Spotify client ID is missing from the helper configuration.");

        var redirectUri = $"http://127.0.0.1:{LoopbackPort}{options.RedirectPath}";
        var codeVerifier = PkceUtility.CreateCodeVerifier();
        var codeChallenge = PkceUtility.CreateCodeChallenge(codeVerifier);
        var state = PkceUtility.CreateState();
        var scope = string.Join(' ', options.Scopes.Where(static scope => !string.IsNullOrWhiteSpace(scope)));

        var authorizationUrl =
            "https://accounts.spotify.com/authorize" +
            $"?client_id={Uri.EscapeDataString(options.ClientId)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&code_challenge_method=S256" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            "&show_dialog=true";

        _pendingRequest = new SpotifyPkceRequest(authorizationUrl, redirectUri, codeVerifier, state, scope);
        return _pendingRequest;
    }

    public async Task<SpotifyAuthResult> RunBrowserFlowAsync(SpotifyAuthOptions options, CancellationToken cancellationToken)
    {
        var request = CreateRequest(options);
        await using var listener = new LoopbackCallbackListener(request.RedirectUri);
        listener.Start();

        Process.Start(new ProcessStartInfo
        {
            FileName = request.AuthorizationUrl,
            UseShellExecute = true
        });

        var callbackUri = await listener.WaitForCallbackAsync(cancellationToken);
        return ValidateCallback(callbackUri, request);
    }

    public SpotifyAuthResult ValidateCallback(Uri callbackUri)
    {
        if (_pendingRequest is null)
            throw new InvalidOperationException("There is no pending auth request to validate.");

        return ValidateCallback(callbackUri, _pendingRequest);
    }

    private static SpotifyAuthResult ValidateCallback(Uri callbackUri, SpotifyPkceRequest request)
    {
        var query = QueryStringUtility.Parse(callbackUri.Query);
        query.TryGetValue("state", out var state);
        query.TryGetValue("error", out var error);
        query.TryGetValue("code", out var code);

        if (!string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException($"Spotify returned an error: {error}.");

        if (!string.Equals(state, request.State, StringComparison.Ordinal))
            throw new InvalidOperationException("Spotify returned an unexpected state value.");

        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Spotify did not return an authorization code.");

        return new SpotifyAuthResult(code, state!, request.RedirectUri, request.CodeVerifier, DateTimeOffset.UtcNow);
    }
}
