using Html2Apk.Models;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Html2Apk.BuildEngine;

public class BuildOrchestrator
{
    private readonly BuildConfig _cfg;
    private readonly string _toolsDir;
    private readonly Action<string> _log;

    // Tools paths — all bundled inside the app in Assets/tools/
    private string GradleWrapper => Path.Combine(_toolsDir, "gradle", "bin", "gradle.bat");
    private string ApkSignerJar  => Path.Combine(_toolsDir, "android-sdk", "build-tools", "34.0.0", "lib", "apksigner.jar");
    private string JavaExe       => Path.Combine(_toolsDir, "jdk", "bin", "java.exe");
    private string Keytool       => Path.Combine(_toolsDir, "jdk", "bin", "keytool.exe");

    public BuildOrchestrator(BuildConfig cfg, string toolsDir, Action<string> log)
    {
        _cfg = cfg;
        _toolsDir = toolsDir;
        _log = log;
    }

    public async Task<string> BuildAsync(CancellationToken ct = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"html2apk_{Guid.NewGuid():N}");
        var appDir  = Path.Combine(workDir, "app");
        var srcDir  = Path.Combine(appDir, "src", "main");
        var javaDir = Path.Combine(srcDir, "java", _cfg.PackageName.Replace('.', Path.DirectorySeparatorChar));
        var assetsDir = Path.Combine(srcDir, "assets");
        var resDir  = Path.Combine(srcDir, "res");

        try
        {
            Directory.CreateDirectory(javaDir);
            Directory.CreateDirectory(assetsDir);
            Directory.CreateDirectory(resDir);

            // 1. Write Gradle project files
            _log("📝 Writing project files...");
            File.WriteAllText(Path.Combine(workDir, "settings.gradle"), GradleFileGenerator.GenerateSettingsGradle(_cfg));
            File.WriteAllText(Path.Combine(appDir,  "build.gradle"),    GradleFileGenerator.GenerateBuildGradle(_cfg));
            File.WriteAllText(Path.Combine(srcDir,  "AndroidManifest.xml"), GradleFileGenerator.GenerateManifest(_cfg));

            // 2. MainActivity.java
            _log("📝 Generating MainActivity.java...");
            File.WriteAllText(Path.Combine(javaDir, "MainActivity.java"), MainActivityGenerator.Generate(_cfg));

            // 3. HTML → assets
            _log("📦 Copying HTML to assets...");
            var htmlContent = await File.ReadAllTextAsync(_cfg.HtmlFilePath, ct);
            await File.WriteAllTextAsync(Path.Combine(assetsDir, "index.html"), htmlContent, ct);

            // 4. Additional files
            foreach (var f in _cfg.AdditionalFiles)
                File.Copy(f, Path.Combine(assetsDir, Path.GetFileName(f)), overwrite: true);

            // 5. Icon → mipmaps
            _log("🎨 Processing icon...");
            IconProcessor.Process(_cfg.IconFilePath, resDir);

            // 6. strings.xml
            var valuesDir = Path.Combine(resDir, "values");
            Directory.CreateDirectory(valuesDir);
            File.WriteAllText(Path.Combine(valuesDir, "strings.xml"),
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?><resources><string name=\"app_name\">{_cfg.AppName}</string></resources>");

            // 7. local.properties — point Gradle to bundled SDK
            var sdkDir = Path.Combine(_toolsDir, "android-sdk").Replace("\\", "\\\\");
            File.WriteAllText(Path.Combine(workDir, "local.properties"), $"sdk.dir={sdkDir}\n");

            // 8. Keystore
            _log("🔑 Preparing keystore...");
            var (ksPath, ksPass, keyPass, alias) = await KeystoreManager.PrepareAsync(_cfg, workDir, Keytool, _log, ct);

            // 9. Run Gradle assembleRelease
            _log("⚙️ Running Gradle build...");
            var javaHome = Path.Combine(_toolsDir, "jdk");
            await RunAsync(GradleWrapper, "assembleRelease", workDir,
                new Dictionary<string, string> { ["JAVA_HOME"] = javaHome }, ct);

            // 10. Find unsigned APK
            var unsignedApk = Directory.GetFiles(appDir, "*.apk", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("release")) 
                ?? throw new FileNotFoundException("Gradle did not produce a release APK.");

            // 11. Sign
            string finalApk;
            if (_cfg.SigningMode == "unsigned")
            {
                finalApk = Path.Combine(_cfg.OutputDir, $"{_cfg.PackageName}.apk");
                File.Copy(unsignedApk, finalApk, overwrite: true);
                _log("✅ APK built (unsigned)");
            }
            else
            {
                finalApk = Path.Combine(_cfg.OutputDir, $"{_cfg.PackageName}.apk");
                _log("✍️ Signing APK...");
                await RunAsync(JavaExe,
                    $"-jar \"{ApkSignerJar}\" sign --ks \"{ksPath}\" --ks-pass pass:{ksPass} --key-pass pass:{keyPass} --ks-key-alias {alias} --out \"{finalApk}\" \"{unsignedApk}\"",
                    workDir, null, ct);
                _log($"✅ Signed APK: {finalApk}");
            }

            return finalApk;
        }
        finally
        {
            // Cleanup temp dir
            try { Directory.Delete(workDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private async Task RunAsync(string exe, string args, string workDir,
        Dictionary<string, string>? envVars, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false,
            CreateNoWindow  = true,
        };
        if (envVars != null)
            foreach (var kv in envVars)
                psi.Environment[kv.Key] = kv.Value;

        using var proc = Process.Start(psi) ?? throw new Exception($"Failed to start {exe}");
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) _log(e.Data); };
        proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) _log("[ERR] " + e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        await proc.WaitForExitAsync(ct);
        if (proc.ExitCode != 0)
            throw new Exception($"{Path.GetFileName(exe)} exited with code {proc.ExitCode}");
    }
}
