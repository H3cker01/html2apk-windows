# Bundled Tools

Place the following tools in this directory before building the app.
The app expects them at `Assets/tools/` relative to the executable.

## Required Structure

```
Assets/tools/
├── jdk/                        # JDK 17 (Windows x64)
│   └── bin/
│       ├── java.exe
│       ├── javac.exe
│       └── keytool.exe
├── gradle/                     # Gradle 8.x
│   └── bin/
│       └── gradle.bat
├── android-sdk/
│   ├── platforms/
│   │   └── android-34/
│   │       └── android.jar
│   └── build-tools/
│       └── 34.0.0/
│           ├── aapt2.exe
│           ├── zipalign.exe
│           └── lib/
│               └── apksigner.jar
└── build-tools/
    └── apksigner.bat           # wrapper: java -jar ../android-sdk/build-tools/34.0.0/lib/apksigner.jar %*

```

## Download Sources

| Tool | Source |
|------|--------|
| JDK 17 | https://adoptium.net/temurin/releases/?version=17 (Windows x64 ZIP) |
| Gradle 8.7 | https://gradle.org/releases/ (binary-only ZIP) |
| Android SDK build-tools | Android Studio SDK Manager or sdkmanager CLI |
| android.jar | Included with Android SDK platform android-34 |

## Google Mobile Ads SDK (AdMob — offline Maven cache)

When AdMob is enabled, Gradle resolves `com.google.android.gms:play-services-ads:23.0.0`
from Maven Central / Google's Maven repo. For fully offline support, pre-populate
the local Gradle cache or set up an offline Maven repository at:
`Assets/tools/offline-repo/`

and add to `settings.gradle`:
```groovy
maven { url file("path/to/Assets/tools/offline-repo") }
```

This will be automated in a future setup wizard.
