using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibreSpotUWPLoginHelper.Services;

internal static class SpotifyAccountEligibilityService
{
    private const string SpotifyMeEndpoint = "https://api.spotify.com/v1/me";
    private static readonly HttpClient HttpClient = new();

    public static async Task EnsurePremiumAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Spotify did not return an access token.");

        using var request = new HttpRequestMessage(HttpMethod.Get, SpotifyMeEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        document.RootElement.TryGetProperty("product", out var productElement);
        var product = productElement.ValueKind == JsonValueKind.String
            ? productElement.GetString()
            : null;

        if (!string.Equals(product, "premium", StringComparison.OrdinalIgnoreCase))
            throw new SpotifyPremiumRequiredException(product);
    }
}
