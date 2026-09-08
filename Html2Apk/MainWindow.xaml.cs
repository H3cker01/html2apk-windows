using Html2Apk.BuildEngine;
using Html2Apk.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Html2Apk;

public partial class MainWindow : Window
{
    private readonly string _toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "tools");

    private static readonly (string key, string label)[] AllPermissions =
    [
        ("internet",          "Internet"),
        ("camera",            "Camera"),
        ("microphone",        "Microphone"),
        ("storage_read",      "Storage Read"),
        ("storage_write",     "Storage Write"),
        ("location_fine",     "Location (Fine)"),
        ("location_coarse",   "Location (Coarse)"),
        ("contacts_read",     "Contacts Read"),
        ("contacts_write",    "Contacts Write"),
        ("phone_state",       "Phone State"),
        ("bluetooth",         "Bluetooth"),
        ("bluetooth_connect", "Bluetooth Connect"),
        ("notifications",     "Notifications"),
        ("vibrate",           "Vibrate"),
        ("nfc",               "NFC"),
        ("biometric",         "Biometric"),
    ];

    public MainWindow()
    {
        InitializeComponent();
        BuildPermissionsPanel();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        PageGeneral.Visibility     = Visibility.Collapsed;
        PagePermissions.Visibility = Visibility.Collapsed;
        PageAdMob.Visibility       = Visibility.Collapsed;
        PageKeystore.Visibility    = Visibility.Collapsed;
        PageOutput.Visibility      = Visibility.Collapsed;

        BtnGeneral.Style     = (Style)FindResource("SideBtn");
        BtnPermissions.Style = (Style)FindResource("SideBtn");
        BtnAdMob.Style       = (Style)FindResource("SideBtn");
        BtnKeystore.Style    = (Style)FindResource("SideBtn");
        BtnOutput.Style      = (Style)FindResource("SideBtn");

        btn.Style = (Style)FindResource("ActiveSideBtn");
        var page = btn.Name switch
        {
            "BtnGeneral"     => (UIElement)PageGeneral,
            "BtnPermissions" => PagePermissions,
            "BtnAdMob"       => PageAdMob,
            "BtnKeystore"    => PageKeystore,
            "BtnOutput"      => PageOutput,
            _                => PageGeneral
        };
        page.Visibility = Visibility.Visible;
    }

    private void BuildPermissionsPanel()
    {
        foreach (var (key, label) in AllPermissions)
        {
            var cb = new CheckBox
            {
                Content   = label,
                Tag       = key,
                Foreground = System.Windows.Media.Brushes.White,
                Margin    = new Thickness(0, 0, 16, 8),
                IsChecked = key == "internet"
            };
            PermPanel.Children.Add(cb);
        }
    }

    private void BrowseHtml_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "HTML files|*.html;*.htm|All files|*.*" };
        if (dlg.ShowDialog() == true) TxtHtml.Text = dlg.FileName;
    }

    private void BrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "PNG files|*.png" };
        if (dlg.ShowDialog() == true) TxtIcon.Text = dlg.FileName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select output folder — pick any file inside it",
            CheckFileExists = false,
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
            TxtOutput.Text = Path.GetDirectoryName(dlg.FileName)!;
    }

    private void BrowseKs_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Keystore files|*.jks;*.keystore|All files|*.*" };
        if (dlg.ShowDialog() == true) TxtKsPath.Text = dlg.FileName;
    }

    private void ChkAdMob_Changed(object sender, RoutedEventArgs e)
    {
        bool on = ChkAdMob.IsChecked == true;
        AdMobFields.IsEnabled = on;
        AdMobFields.Opacity   = on ? 1.0 : 0.5;
    }

    private void CmbSignMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (KeystoreFields == null) return;
        var tag = ((ComboBoxItem)CmbSignMode.SelectedItem)?.Tag?.ToString() ?? "debug";
        KeystoreFields.Visibility = tag == "unsigned" ? Visibility.Collapsed : Visibility.Visible;
        if (TxtKsPath != null)
            TxtKsPath.IsReadOnly = tag != "upload";
    }

    private async void BtnBuild_Click(object sender, RoutedEventArgs e)
    {
        var cfg = CollectConfig();
        if (!Validate(cfg)) return;

        BtnBuild.IsEnabled = false;
        BuildProgress.Visibility = Visibility.Visible;
        TxtLog.Text = "";
        Log("🚀 Starting build...");

        try
        {
            var engine = new BuildOrchestrator(cfg, _toolsDir, Log);
            var apk    = await engine.BuildAsync();
            Log($"\n✅ Done! APK saved to:\n{apk}");
            MessageBox.Show($"Build complete!\n{apk}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"\n❌ Build failed: {ex.Message}");
            MessageBox.Show($"Build failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnBuild.IsEnabled = true;
            BuildProgress.Visibility = Visibility.Collapsed;
        }
    }

    private BuildConfig CollectConfig()
    {
        var perms = new HashSet<string>();
        foreach (CheckBox cb in PermPanel.Children)
            if (cb.IsChecked == true) perms.Add((string)cb.Tag);

        var signTag      = ((ComboBoxItem)CmbSignMode.SelectedItem)?.Tag?.ToString() ?? "debug";
        var buildTypeTag = ((ComboBoxItem)CmbBuildType.SelectedItem)?.Tag?.ToString() ?? "apk";

        return new BuildConfig
        {
            AppName      = TxtAppName.Text.Trim(),
            PackageName  = TxtPackage.Text.Trim(),
            HtmlFilePath = TxtHtml.Text.Trim(),
            IconFilePath = TxtIcon.Text.Trim(),
            OutputDir    = TxtOutput.Text.Trim(),
            Permissions  = perms,
            AdMobEnabled         = ChkAdMob.IsChecked == true,
            AdMobAppId           = TxtAdMobAppId.Text.Trim(),
            BannerAdUnitId       = TxtBannerAdUnit.Text.Trim(),
            InterstitialAdUnitId = TxtInterstitialAdUnit.Text.Trim(),
            SigningMode      = signTag,
            KeystorePath     = TxtKsPath.Text.Trim(),
            KeystoreAlias    = TxtKsAlias.Text.Trim(),
            KeystorePassword = TxtKsPass.Password,
            KeyPassword      = TxtKeyPass.Password,
            BuildType        = buildTypeTag,
        };
    }

    private bool Validate(BuildConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.HtmlFilePath) || !File.Exists(cfg.HtmlFilePath))
        { MessageBox.Show("Please select a valid HTML file.", "Validation"); return false; }
        if (string.IsNullOrWhiteSpace(cfg.OutputDir))
        { MessageBox.Show("Please select an output folder.", "Validation"); return false; }
        if (string.IsNullOrWhiteSpace(cfg.AppName))
        { MessageBox.Show("App name is required.", "Validation"); return false; }
        if (!System.Text.RegularExpressions.Regex.IsMatch(cfg.PackageName, @"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$"))
        { MessageBox.Show("Invalid package name (e.g. com.example.myapp).", "Validation"); return false; }
        if (cfg.SigningMode is "generate" or "upload" && cfg.KeystorePassword.Length < 6)
        { MessageBox.Show("Keystore password must be at least 6 characters.", "Validation"); return false; }
        if (cfg.AdMobEnabled && string.IsNullOrWhiteSpace(cfg.AdMobAppId))
        { MessageBox.Show("AdMob App ID is required when AdMob is enabled.", "Validation"); return false; }
        return true;
    }

    private void Log(string msg)
    {
        Dispatcher.Invoke(() =>
        {
            TxtLog.Text += msg + "\n";
            LogScroll.ScrollToBottom();
        });
    }
}
