using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using LibreSpotUWPLoginHelper.Models;
using SpotifyAPI.Web;

namespace LibreSpotUWPLoginHelper.Services;

internal sealed class SpotifyTokenExchangeService
{
    private const int CurrentScopeVersion = 4;
    private const int CurrentAuthVersion = 1;
    private const int CurrentPlaybackAuthVersion = 2;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<WebAuthorizationResult> ExchangeCodeAsync(string clientId, SpotifyAuthResult authResult)
    {
        var request = new PKCETokenRequest(
            clientId,
            authResult.Code,
            new Uri(authResult.RedirectUri),
            authResult.CodeVerifier);

        var oauth = new OAuthClient();
        var response = await oauth.RequestToken(request);
        var capturedAt = DateTimeOffset.UtcNow;

        var accountId = await SpotifyAccountEligibilityService.GetPremiumAccountIdAsync(response.AccessToken);

        return new WebAuthorizationResult(new QrAuthState
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken ?? string.Empty,
            ClientId = clientId,
            ExpiresAt = capturedAt.AddSeconds(response.ExpiresIn),
            LastTokenRefreshAt = capturedAt,
            RefreshTokenExpiresAt = TryGetRefreshTokenExpiresAt(response, capturedAt),
            ScopeVersion = CurrentScopeVersion,
            AuthVersion = CurrentAuthVersion
        }, accountId);
    }

    public async Task<PlaybackAuthorizationPackage> ExchangePlaybackCodeAsync(
        string clientId,
        SpotifyAuthResult authResult,
        string expectedAccountId)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authResult.Code,
            ["redirect_uri"] = authResult.RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = authResult.CodeVerifier
        });
        using var response = await HttpClient.PostAsync("https://accounts.spotify.com/api/token", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Spotify refused playback authorization ({(int)response.StatusCode}).");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("access_token", out var tokenElement) ||
            tokenElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            throw new InvalidOperationException("Spotify did not return a playback access token.");
        }

        var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) &&
            expiresElement.TryGetInt32(out var seconds)
                ? seconds
                : 3600;

        var accessToken = tokenElement.GetString()!;
        var playbackAccountId = await SpotifyAccountEligibilityService
            .GetPremiumAccountIdAsync(accessToken);
        if (!string.Equals(expectedAccountId, playbackAccountId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Playback was authorized with a different Spotify account. Start again and select the same account in both browser windows.");
        }

        return new PlaybackAuthorizationPackage
        {
            AuthVersion = CurrentPlaybackAuthVersion,
            Kind = "bootstrapToken",
            AccessToken = accessToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        };
    }

    private static DateTimeOffset? TryGetRefreshTokenExpiresAt(object response, DateTimeOffset capturedAt)
    {
        var property = response.GetType().GetRuntimeProperty("RefreshTokenExpiresIn");
        if (property?.GetValue(response) is int secondsInt && secondsInt > 0)
            return capturedAt.AddSeconds(secondsInt);

        if (property?.GetValue(response) is long secondsLong && secondsLong > 0)
            return capturedAt.AddSeconds(secondsLong);

        if (property?.GetValue(response) is double secondsDouble && secondsDouble > 0)
            return capturedAt.AddSeconds(secondsDouble);

        return null;
    }
}

internal sealed record WebAuthorizationResult(QrAuthState State, string AccountId);
