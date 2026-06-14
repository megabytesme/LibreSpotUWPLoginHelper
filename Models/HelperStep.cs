namespace LibreSpotUWPLoginHelper.Models;

internal sealed record HelperStep(
    string Eyebrow,
    string Title,
    string Summary,
    string PrimaryCardTitle,
    string PrimaryCardBody,
    string SecondaryCardTitle,
    string SecondaryCardBody,
    string CalloutTitle,
    string CalloutBody,
    string FooterHint,
    string NextLabel,
    bool ShowsBrowserAction = false,
    string BrowserActionLabel = "");
