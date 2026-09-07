using Html2Apk.Models;

namespace Html2Apk.BuildEngine;

public static class GradleFileGenerator
{
    public static string GenerateBuildGradle(BuildConfig cfg)
    {
        string admobDep = cfg.AdMobEnabled
            ? "    implementation 'com.google.android.gms:play-services-ads:23.0.0'"
            : "";

        string admobMeta = cfg.AdMobEnabled
            ? $@"        <meta-data
            android:name=""com.google.android.gms.ads.APPLICATION_ID""
            android:value=""{cfg.AdMobAppId}""/>"
            : "";

        return $@"plugins {{
    id 'com.android.application'
}}

android {{
    compileSdk 34
    namespace '{cfg.PackageName}'
    defaultConfig {{
        applicationId '{cfg.PackageName}'
        minSdk 21
        targetSdk 34
        versionCode 1
        versionName '1.0'
    }}
    buildTypes {{
        release {{
            minifyEnabled false
        }}
    }}
    compileOptions {{
        sourceCompatibility JavaVersion.VERSION_1_8
        targetCompatibility JavaVersion.VERSION_1_8
    }}
}}

dependencies {{
{admobDep}
}}
";
    }

    public static string GenerateSettingsGradle(BuildConfig cfg) => $@"pluginManagement {{
    repositories {{
        google()
        mavenCentral()
        gradlePluginPortal()
    }}
}}
dependencyResolutionManagement {{
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {{
        google()
        mavenCentral()
    }}
}}
rootProject.name = '{cfg.AppName}'
include ':app'
";

    public static string GenerateManifest(BuildConfig cfg)
    {
        var permMap = new Dictionary<string, string>
        {
            ["internet"]          = "android.permission.INTERNET",
            ["camera"]            = "android.permission.CAMERA",
            ["microphone"]        = "android.permission.RECORD_AUDIO",
            ["storage_read"]      = "android.permission.READ_EXTERNAL_STORAGE",
            ["storage_write"]     = "android.permission.WRITE_EXTERNAL_STORAGE",
            ["location_fine"]     = "android.permission.ACCESS_FINE_LOCATION",
            ["location_coarse"]   = "android.permission.ACCESS_COARSE_LOCATION",
            ["contacts_read"]     = "android.permission.READ_CONTACTS",
            ["contacts_write"]    = "android.permission.WRITE_CONTACTS",
            ["phone_state"]       = "android.permission.READ_PHONE_STATE",
            ["bluetooth"]         = "android.permission.BLUETOOTH",
            ["bluetooth_connect"] = "android.permission.BLUETOOTH_CONNECT",
            ["notifications"]     = "android.permission.POST_NOTIFICATIONS",
            ["vibrate"]           = "android.permission.VIBRATE",
            ["nfc"]               = "android.permission.NFC",
            ["biometric"]         = "android.permission.USE_BIOMETRIC",
        };

        var perms = new HashSet<string>(cfg.Permissions) { "internet" };
        var permXml = string.Join("\n    ", perms
            .Where(p => permMap.ContainsKey(p))
            .Select(p => $@"<uses-permission android:name=""{permMap[p]}""/>"));

        string admobMeta = cfg.AdMobEnabled
            ? $@"
            <meta-data android:name=""com.google.android.gms.ads.APPLICATION_ID"" android:value=""{cfg.AdMobAppId}""/>"
            : "";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
    <uses-sdk android:minSdkVersion=""21"" android:targetSdkVersion=""34""/>
    {permXml}
    <application android:label=""{cfg.AppName}"" android:icon=""@mipmap/ic_launcher""
        android:theme=""@android:style/Theme.NoTitleBar.Fullscreen""
        android:allowBackup=""true"" android:supportsRtl=""true"">{admobMeta}
        <activity android:name="".MainActivity"" android:exported=""true""
            android:configChanges=""orientation|screenSize|keyboardHidden"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN""/>
                <category android:name=""android.intent.category.LAUNCHER""/>
            </intent-filter>
        </activity>
    </application>
</manifest>
";
    }
}
