namespace LibreSpotUWPLoginHelper.Models;

internal sealed record SpotifyPkceRequest(
    string AuthorizationUrl,
    string RedirectUri,
    string CodeVerifier,
    string State,
    string Scope);
