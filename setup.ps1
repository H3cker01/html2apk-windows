# HTML2Apk Windows - Setup Script
# Run as: powershell -ExecutionPolicy Bypass -File setup.ps1

$ErrorActionPreference = "Stop"
$ProgressPreference    = "SilentlyContinue"   # speeds up Invoke-WebRequest

$RepoUrl   = "https://github.com/H3cker01/html2apk-windows.git"
$InstallDir = "$env:USERPROFILE\html2apk-windows"
$ToolsDir  = "$InstallDir\Html2Apk\Assets\tools"

function Log($msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Ok($msg)  { Write-Host "  OK $msg" -ForegroundColor Green }
function Die($msg) { Write-Host "  ERROR: $msg" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "================================================" -ForegroundColor Magenta
Write-Host "   HTML2Apk for Windows - Setup" -ForegroundColor Magenta
Write-Host "================================================" -ForegroundColor Magenta
Write-Host ""

# ── 0. Check prerequisites ────────────────────────────────────────────────
Log "Checking prerequisites..."

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Die "Git is not installed. Install from https://git-scm.com and re-run."
}

# ── 1. Clone repo ─────────────────────────────────────────────────────────
if (Test-Path $InstallDir) {
    Log "Repo already exists at $InstallDir — pulling latest..."
    git -C $InstallDir pull --quiet
} else {
    Log "Cloning repo to $InstallDir ..."
    git clone $RepoUrl $InstallDir --quiet
}
Ok "Repo ready"

# ── Helper: download + unzip ──────────────────────────────────────────────
function Download($url, $dest) {
    Log "Downloading $(Split-Path $url -Leaf) ..."
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}

function Unzip($zip, $dest) {
    Log "Extracting to $dest ..."
    Expand-Archive -Path $zip -DestinationPath $dest -Force
    Remove-Item $zip -Force
}

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null
$tmp = "$env:TEMP\html2apk_setup"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

# ── 2. JDK 17 ─────────────────────────────────────────────────────────────
$JdkDir = "$ToolsDir\jdk"
if (-not (Test-Path "$JdkDir\bin\java.exe")) {
    Write-Host ""
    Write-Host "  [1/4] JDK 17" -ForegroundColor Yellow
    $jdkZip = "$tmp\jdk17.zip"
    Download "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.11%2B9/OpenJDK17U-jdk_x64_windows_hotspot_17.0.11_9.zip" $jdkZip
    Unzip $jdkZip "$tmp\jdk17_extracted"
    # Adoptium extracts into a versioned subfolder — find it
    $jdkExtracted = Get-ChildItem "$tmp\jdk17_extracted" -Directory | Select-Object -First 1
    if ($jdkExtracted) {
        Move-Item $jdkExtracted.FullName $JdkDir -Force
    } else { Die "JDK extraction failed" }
    Ok "JDK 17 installed"
} else { Ok "JDK 17 already present" }

# ── 3. Gradle 8.7 ─────────────────────────────────────────────────────────
$GradleDir = "$ToolsDir\gradle"
if (-not (Test-Path "$GradleDir\bin\gradle.bat")) {
    Write-Host ""
    Write-Host "  [2/4] Gradle 8.7" -ForegroundColor Yellow
    $gradleZip = "$tmp\gradle.zip"
    Download "https://services.gradle.org/distributions/gradle-8.7-bin.zip" $gradleZip
    Unzip $gradleZip "$tmp\gradle_extracted"
    $gradleExtracted = Get-ChildItem "$tmp\gradle_extracted" -Directory | Select-Object -First 1
    if ($gradleExtracted) {
        Move-Item $gradleExtracted.FullName $GradleDir -Force
    } else { Die "Gradle extraction failed" }
    Ok "Gradle 8.7 installed"
} else { Ok "Gradle already present" }

# ── 4. Command-line tools (sdkmanager) ────────────────────────────────────
$SdkDir       = "$ToolsDir\android-sdk"
$CmdlineTools = "$SdkDir\cmdline-tools\latest"
$SdkManager   = "$CmdlineTools\bin\sdkmanager.bat"

if (-not (Test-Path $SdkManager)) {
    Write-Host ""
    Write-Host "  [3/4] Android Command-line Tools" -ForegroundColor Yellow
    $ctZip = "$tmp\cmdline-tools.zip"
    Download "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip" $ctZip
    New-Item -ItemType Directory -Force -Path $CmdlineTools | Out-Null
    # cmdline-tools zip extracts into a 'cmdline-tools' subfolder
    Unzip $ctZip "$tmp\ct_extracted"
    $ctExtracted = "$tmp\ct_extracted\cmdline-tools"
    if (Test-Path $ctExtracted) {
        Get-ChildItem $ctExtracted | Move-Item -Destination $CmdlineTools -Force
    } else { Die "cmdline-tools extraction failed" }
    Ok "Android cmdline-tools installed"
} else { Ok "Android cmdline-tools already present" }

# ── 5. Accept licenses + install platform + build-tools via sdkmanager ────
Write-Host ""
Write-Host "  [4/4] Android SDK packages (platform-34, build-tools-34)" -ForegroundColor Yellow
$env:JAVA_HOME = $JdkDir
$env:Path      = "$JdkDir\bin;$env:Path"

# Accept licenses non-interactively
Log "Accepting Android SDK licenses..."
"y`ny`ny`ny`ny`ny`ny`n" | & "$SdkManager" --sdk_root="$SdkDir" --licenses 2>&1 | Out-Null

Log "Installing android-34 platform..."
& "$SdkManager" --sdk_root="$SdkDir" "platforms;android-34" 2>&1 | ForEach-Object { if ($_ -match '\[') { Write-Host "    $_" } }

Log "Installing build-tools 34.0.0..."
& "$SdkManager" --sdk_root="$SdkDir" "build-tools;34.0.0" 2>&1 | ForEach-Object { if ($_ -match '\[') { Write-Host "    $_" } }

Ok "Android SDK packages installed"

# ── 6. apksigner.bat wrapper ───────────────────────────────────────────────
$btDir      = "$SdkDir\build-tools\34.0.0"
$wrapperDir = "$ToolsDir\build-tools"
New-Item -ItemType Directory -Force -Path $wrapperDir | Out-Null

$apkSignerJar = "$btDir\lib\apksigner.jar"
$wrapperBat   = "$wrapperDir\apksigner.bat"

$wrapperContent = "@echo off`r`n`"$JdkDir\bin\java.exe`" -jar `"$apkSignerJar`" %*"
Set-Content -Path $wrapperBat -Value $wrapperContent -Encoding ASCII
Ok "apksigner.bat wrapper created"

# ── 7. local.properties for the IDE (optional convenience) ────────────────
$localProps = "$InstallDir\local.properties"
Set-Content -Path $localProps -Value "sdk.dir=$($SdkDir -replace '\\','/')" -Encoding UTF8
Ok "local.properties written"

# ── Done ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "   Setup complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Project : $InstallDir" -ForegroundColor White
Write-Host "  Tools   : $ToolsDir" -ForegroundColor White
Write-Host ""
Write-Host "  Next: Open Html2Apk.sln in Visual Studio 2022" -ForegroundColor White
Write-Host "        Build -> Run and start converting HTML to APK!" -ForegroundColor White
Write-Host ""
