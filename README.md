# HTML2Apk for Windows

Offline Windows desktop app that converts HTML files into signed Android APKs — no internet required after install.

## Features
- Full offline build pipeline (Gradle + JDK + Android build-tools bundled)
- AdMob integration (banner & interstitial) — just paste your IDs
- Custom app icon, package name, permissions manager
- Keystore: debug / generate / upload your own
- APK or AAB output
- MS Store ready (MSIX packaged)

## Tech Stack
- WPF .NET 8
- Bundled: JDK 17, Gradle 8, Android build-tools, android.jar, Google Mobile Ads SDK AAR

## Project Structure
```
html2apk-windows/
├── Html2Apk.sln
├── Html2Apk/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── BuildEngine/
│   │   ├── BuildOrchestrator.cs      # Main build runner
│   │   ├── ManifestGenerator.cs      # AndroidManifest.xml generator
│   │   ├── MainActivityGenerator.cs  # Dynamic MainActivity.java (+ AdMob)
│   │   ├── GradleFileGenerator.cs    # build.gradle + settings.gradle
│   │   └── KeystoreManager.cs        # Keystore logic
│   ├── Models/
│   │   └── BuildConfig.cs            # All user settings
│   ├── Views/
│   │   ├── GeneralPage.xaml
│   │   ├── PermissionsPage.xaml
│   │   ├── AdMobPage.xaml
│   │   └── KeystorePage.xaml
│   └── Assets/                       # Bundled tools placeholder
│       └── README.md
└── README.md
```

## Build
Requires Visual Studio 2022 with .NET 8 desktop workload.

Bundled tools go in `Html2Apk/Assets/tools/` — see `Assets/README.md`.
