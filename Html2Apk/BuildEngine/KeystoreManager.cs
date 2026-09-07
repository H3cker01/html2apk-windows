using Html2Apk.Models;
using System.Diagnostics;
using System.IO;

namespace Html2Apk.BuildEngine;

public static class KeystoreManager
{
    public static async Task<(string ksPath, string ksPass, string keyPass, string alias)>
        PrepareAsync(BuildConfig cfg, string workDir, string keytool, Action<string> log, CancellationToken ct)
    {
        switch (cfg.SigningMode)
        {
            case "unsigned":
                return ("", "", "", "");

            case "upload":
                log($"🔑 Using uploaded keystore, alias={cfg.KeystoreAlias}");
                return (cfg.KeystorePath, cfg.KeystorePassword, cfg.KeyPassword, cfg.KeystoreAlias);

            case "generate":
            {
                var ksPath = Path.Combine(workDir, "generated.keystore");
                log($"🔑 Generating keystore, alias={cfg.KeystoreAlias}");
                await RunKeytool(keytool,
                    $"-genkeypair -v -keystore \"{ksPath}\" -alias {cfg.KeystoreAlias} " +
                    $"-keyalg RSA -keysize 2048 -validity 10000 " +
                    $"-storepass {cfg.KeystorePassword} -keypass {cfg.KeyPassword} " +
                    $"-dname \"CN={cfg.AppName},O=Android,C=US\"",
                    workDir, ct);
                return (ksPath, cfg.KeystorePassword, cfg.KeyPassword, cfg.KeystoreAlias);
            }

            default: // debug
            {
                var ksPath = Path.Combine(workDir, "debug.keystore");
                log("🔑 Using auto debug keystore");
                await RunKeytool(keytool,
                    $"-genkeypair -v -keystore \"{ksPath}\" -alias androiddebugkey " +
                    "-keyalg RSA -keysize 2048 -validity 10000 " +
                    "-storepass android -keypass android " +
                    "-dname \"CN=Debug,O=Android,C=US\"",
                    workDir, ct);
                return (ksPath, "android", "android", "androiddebugkey");
            }
        }
    }

    private static async Task RunKeytool(string keytool, string args, string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(keytool, args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false,
            CreateNoWindow  = true,
        };
        using var proc = Process.Start(psi) ?? throw new Exception("Failed to start keytool");
        await proc.WaitForExitAsync(ct);
        if (proc.ExitCode != 0)
            throw new Exception($"keytool failed with code {proc.ExitCode}");
    }
}
