using System;
using System.Reflection;
using System.Threading.Tasks;
using LibreSpotUWPLoginHelper.Models;
using SpotifyAPI.Web;

namespace LibreSpotUWPLoginHelper.Services;

internal sealed class SpotifyTokenExchangeService
{
    public async Task<QrAuthState> ExchangeCodeAsync(string clientId, SpotifyAuthResult authResult)
    {
        var request = new PKCETokenRequest(
            clientId,
            authResult.Code,
            new Uri(authResult.RedirectUri),
            authResult.CodeVerifier);

        var oauth = new OAuthClient();
        var response = await oauth.RequestToken(request);
        var capturedAt = DateTimeOffset.UtcNow;

        return new QrAuthState
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken ?? string.Empty,
            ExpiresAt = capturedAt.AddSeconds(response.ExpiresIn),
            LastTokenRefreshAt = capturedAt,
            RefreshTokenExpiresAt = TryGetRefreshTokenExpiresAt(response, capturedAt)
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
