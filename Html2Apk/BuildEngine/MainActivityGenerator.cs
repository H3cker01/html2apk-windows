using Html2Apk.Models;

namespace Html2Apk.BuildEngine;

public static class MainActivityGenerator
{
    public static string Generate(BuildConfig cfg)
    {
        string admobImports = cfg.AdMobEnabled ? @"
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.AdSize;
import com.google.android.gms.ads.AdView;
import com.google.android.gms.ads.MobileAds;
import com.google.android.gms.ads.interstitial.InterstitialAd;
import com.google.android.gms.ads.interstitial.InterstitialAdLoadCallback;
import com.google.android.gms.ads.LoadAdError;
import android.widget.LinearLayout;
import android.widget.FrameLayout;" : "";

        string admobFields = cfg.AdMobEnabled ? @"
    private AdView mAdView;
    private InterstitialAd mInterstitialAd;" : "";

        string admobInit = cfg.AdMobEnabled ? $@"
            MobileAds.initialize(this, initializationStatus -> {{}});
            // Banner Ad
            LinearLayout layout = new LinearLayout(this);
            layout.setOrientation(LinearLayout.VERTICAL);
            mAdView = new AdView(this);
            mAdView.setAdSize(AdSize.BANNER);
            mAdView.setAdUnitId(""{cfg.BannerAdUnitId}"");
            layout.addView(wv);
            layout.addView(mAdView);
            setContentView(layout);
            mAdView.loadAd(new AdRequest.Builder().build());
            // Interstitial Ad
            InterstitialAd.load(this, ""{cfg.InterstitialAdUnitId}"", new AdRequest.Builder().build(),
                new InterstitialAdLoadCallback() {{
                    @Override public void onAdLoaded(InterstitialAd ad) {{ mInterstitialAd = ad; }}
                    @Override public void onAdFailedToLoad(LoadAdError e) {{ mInterstitialAd = null; }}
                }});" : @"
            setContentView(wv);";

        // If AdMob is enabled, setContentView is handled inside admobInit
        string setContent = cfg.AdMobEnabled ? "" : "            setContentView(wv);";

        return $@"package {cfg.PackageName};
import android.app.Activity;
import android.os.Build;
import android.os.Bundle;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.webkit.GeolocationPermissions;
import android.webkit.PermissionRequest;
import android.view.WindowManager;
import android.content.pm.PackageManager;
import java.util.ArrayList;{admobImports}
public class MainActivity extends Activity {{
    private WebView wv;
    private android.webkit.ValueCallback<android.net.Uri[]> fileChooserCallback;
    private static final int PERM_REQ = 1001;{admobFields}
    @Override protected void onCreate(Bundle s) {{
        super.onCreate(s);
        if (getActionBar() != null) getActionBar().hide();
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        wv = new WebView(this);
        WebSettings ws = wv.getSettings();
        ws.setJavaScriptEnabled(true); ws.setDomStorageEnabled(true);
        ws.setAllowFileAccessFromFileURLs(true); ws.setAllowUniversalAccessFromFileURLs(true);
        ws.setMediaPlaybackRequiresUserGesture(false);
        ws.setAllowFileAccess(true); ws.setAllowContentAccess(true);
        ws.setDatabaseEnabled(true); ws.setGeolocationEnabled(true);
        ws.setCacheMode(android.webkit.WebSettings.LOAD_DEFAULT);
        wv.setWebViewClient(new WebViewClient());
        wv.setWebChromeClient(new android.webkit.WebChromeClient() {{
            @Override public void onPermissionRequest(PermissionRequest req) {{ req.grant(req.getResources()); }}
            @Override public void onGeolocationPermissionsShowPrompt(String origin, GeolocationPermissions.Callback cb) {{ cb.invoke(origin, true, false); }}
            @Override public boolean onShowFileChooser(WebView v, android.webkit.ValueCallback<android.net.Uri[]> cb, android.webkit.WebChromeClient.FileChooserParams p) {{
                fileChooserCallback = cb;
                android.content.Intent intent = p.createIntent();
                try {{ startActivityForResult(intent, 2001); }} catch (Exception e) {{ fileChooserCallback = null; return false; }}
                return true;
            }}
        }});
        wv.setDownloadListener((url, ua, cd, mime, len) -> {{
            android.content.Intent i = new android.content.Intent(android.content.Intent.ACTION_VIEW);
            i.setData(android.net.Uri.parse(url));
            startActivity(i);
        }});{admobInit}
        requestRuntimePerms();
        wv.loadUrl(""file:///android_asset/index.html"");
    }}
    private void requestRuntimePerms() {{
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) return;
        String[] allPerms = {{
            ""android.permission.CAMERA"", ""android.permission.RECORD_AUDIO"",
            ""android.permission.READ_EXTERNAL_STORAGE"", ""android.permission.WRITE_EXTERNAL_STORAGE"",
            ""android.permission.READ_MEDIA_IMAGES"", ""android.permission.ACCESS_FINE_LOCATION"",
            ""android.permission.ACCESS_COARSE_LOCATION"", ""android.permission.READ_CONTACTS"",
            ""android.permission.WRITE_CONTACTS"", ""android.permission.POST_NOTIFICATIONS"",
            ""android.permission.BLUETOOTH_CONNECT"", ""android.permission.USE_BIOMETRIC"",
            ""android.permission.NFC""
        }};
        ArrayList<String> needed = new ArrayList<>();
        for (String p : allPerms) {{
            try {{ if (checkSelfPermission(p) != PackageManager.PERMISSION_GRANTED) needed.add(p); }} catch (Exception ignored) {{}}
        }}
        if (!needed.isEmpty()) requestPermissions(needed.toArray(new String[0]), PERM_REQ);
    }}
    @Override public void onBackPressed() {{ if (wv != null && wv.canGoBack()) wv.goBack(); else super.onBackPressed(); }}
    @Override protected void onActivityResult(int req, int res, android.content.Intent data) {{
        if (req == 2001) {{
            android.webkit.ValueCallback<android.net.Uri[]> cb = fileChooserCallback;
            fileChooserCallback = null;
            if (cb != null) {{
                android.net.Uri[] results = null;
                if (res == RESULT_OK && data != null) {{
                    String dataStr = data.getDataString();
                    if (dataStr != null) results = new android.net.Uri[]{{android.net.Uri.parse(dataStr)}};
                    else if (data.getClipData() != null) {{
                        int count = data.getClipData().getItemCount();
                        results = new android.net.Uri[count];
                        for (int i = 0; i < count; i++) results[i] = data.getClipData().getItemAt(i).getUri();
                    }}
                }}
                cb.onReceiveValue(results);
            }}
        }}
    }}
}}
";
    }
}
