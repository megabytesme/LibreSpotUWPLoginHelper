using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using LibreSpotUWPLoginHelper.Models;
using LibreSpotUWPLoginHelper.Services;
using Windows.Storage.Streams;

namespace LibreSpotUWPLoginHelper;

public sealed partial class MainWindow : Window
{
    private const string SpotifyClientId = "203834fb201e47df89a18d8291b5dea1";
    private const string ProjectUrl = "https://github.com/megabytesme/LibreSpotUWP";
    private const int PageCount = 3;
    private readonly SpotifyAuthBroker _authBroker = new();
    private readonly SpotifyTokenExchangeService _tokenExchangeService = new();
    private CancellationTokenSource? _authCancellationTokenSource;
    private SpotifyAuthResult? _authResult;
    private QrAuthState? _qrAuthState;
    private int _currentPageIndex;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(HelperTitleBar);
        TryApplyWindowIcon();
        ShowPage(0);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex > 0)
            ShowPage(_currentPageIndex - 1);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex < PageCount - 1)
            ShowPage(_currentPageIndex + 1);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        LoginNextButton.IsEnabled = false;
        _authCancellationTokenSource?.Cancel();
        _authCancellationTokenSource = new CancellationTokenSource();

        try
        {
            SetLoginStatus("Opening browser", "Opening Spotify in your default browser and waiting for sign-in to complete.", InfoBarSeverity.Informational);

            _authResult = await _authBroker.RunBrowserFlowAsync(BuildSpotifyAuthOptions(), _authCancellationTokenSource.Token);
            SetLoginStatus("Finalizing session", "Spotify sign-in completed. Exchanging the authorization code for a QR-importable session.", InfoBarSeverity.Informational);

            _qrAuthState = await _tokenExchangeService.ExchangeCodeAsync(SpotifyClientId, _authResult);
            var qrPayload = BuildQrPayload(_qrAuthState);

            var qrImage = await CreateQrImageAsync(qrPayload);
            QrCodeImage.Source = qrImage;
            QrOverlayImage.Source = qrImage;

            LoginNextButton.IsEnabled = true;
            SetLoginStatus("Spotify connected", "Spotify sign-in completed, ready to continue.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            SetLoginStatus("Sign-in incomplete", ex.Message, InfoBarSeverity.Warning);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void QrButton_Click(object sender, RoutedEventArgs e)
    {
        if (QrCodeImage.Source is null)
            return;

        UpdateOverlaySize();
        QrOverlay.Visibility = Visibility.Visible;
    }

    private void CloseQrOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        QrOverlay.Visibility = Visibility.Collapsed;
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ProjectLinkButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = ProjectUrl,
            UseShellExecute = true
        });
    }

    private void SetLoginStatus(string title, string message, InfoBarSeverity severity)
    {
        LoginStatusInfoBar.Title = title;
        LoginStatusInfoBar.Message = message;
        LoginStatusInfoBar.Severity = severity;
        LoginStatusInfoBar.IsOpen = true;
    }

    private static SpotifyAuthOptions BuildSpotifyAuthOptions()
    {
        return new SpotifyAuthOptions(
            SpotifyClientId,
            new[]
            {
                "user-read-email",
                "user-read-private",
                "playlist-read-private",
                "playlist-read-collaborative",
                "streaming"
                ,
                "user-read-recently-played",
                "user-top-read",
                "user-library-read",
                "user-read-playback-state",
                "user-modify-playback-state",
                "user-read-currently-playing",
                "user-follow-read"
            });
    }

    private static string BuildQrPayload(QrAuthState authState)
    {
        return JsonSerializer.Serialize(authState);
    }

    private static async System.Threading.Tasks.Task<BitmapImage> CreateQrImageAsync(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        var bytes = pngQr.GetGraphic(20);

        var image = new BitmapImage();
        using var memoryStream = new InMemoryRandomAccessStream();
        await memoryStream.WriteAsync(bytes.AsBuffer());
        memoryStream.Seek(0);
        await image.SetSourceAsync(memoryStream);
        return image;
    }

    private void UpdateOverlaySize()
    {
        QrOverlayHost.Width = Math.Max(Bounds.Width * 0.8, 320);
        QrOverlayHost.Height = Math.Max(Bounds.Height * 0.8, 320);
    }

    private void ShowPage(int pageIndex)
    {
        _currentPageIndex = Math.Max(0, Math.Min(PageCount - 1, pageIndex));
        WelcomePage.Visibility = _currentPageIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        SpotifyLoginPage.Visibility = _currentPageIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        QrLoginPage.Visibility = _currentPageIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TryApplyWindowIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LibreSpotUWP.ico");
            AppWindow.SetIcon(iconPath);
        }
        catch
        {
        }
    }
}
