namespace Html2Apk.Models;

public class BuildConfig
{
    public string AppName { get; set; } = "MyApp";
    public string PackageName { get; set; } = "com.example.myapp";
    public string HtmlFilePath { get; set; } = "";
    public string IconFilePath { get; set; } = "";
    public List<string> AdditionalFiles { get; set; } = new();
    public HashSet<string> Permissions { get; set; } = new() { "internet" };

    // AdMob
    public bool AdMobEnabled { get; set; } = false;
    public string AdMobAppId { get; set; } = "";
    public string BannerAdUnitId { get; set; } = "";
    public string InterstitialAdUnitId { get; set; } = "";

    // Keystore
    public string SigningMode { get; set; } = "debug"; // debug | generate | upload | unsigned
    public string KeystorePath { get; set; } = "";
    public string KeystoreAlias { get; set; } = "mykey";
    public string KeystorePassword { get; set; } = "";
    public string KeyPassword { get; set; } = "";

    // Output
    public string BuildType { get; set; } = "apk"; // apk | aab
    public string OutputDir { get; set; } = "";
}
