using System;

namespace LibreSpotUWPLoginHelper.Models;

internal sealed record SpotifyAuthResult(
    string Code,
    string State,
    string RedirectUri,
    string CodeVerifier,
    DateTimeOffset CapturedAtUtc);
