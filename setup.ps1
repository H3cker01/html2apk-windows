# HTML2Apk Windows - Setup Script
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$RepoUrl    = "https://github.com/H3cker01/html2apk-windows.git"
$InstallDir = "$env:USERPROFILE\html2apk-windows"
$ToolsDir   = "$InstallDir\Html2Apk\Assets\tools"
$tmp        = "$env:TEMP\html2apk_setup"

function Log($msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Ok($msg)  { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Die($msg) { Write-Host "  [ERROR] $msg" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "  HTML2Apk for Windows - Setup" -ForegroundColor Magenta
Write-Host ""

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Die "Git not found. Install from https://git-scm.com and re-run."
}

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

if (Test-Path "$InstallDir\.git") {
    Log "Repo exists, pulling latest..."
    git -C $InstallDir pull --quiet
} else {
    Log "Cloning repo..."
    git clone $RepoUrl $InstallDir --quiet
}
Ok "Repo ready at $InstallDir"

function Download($url, $dest) {
    Log "Downloading $(Split-Path $url -Leaf)..."
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}

function Unzip($zip, $dest) {
    Log "Extracting..."
    Expand-Archive -Path $zip -DestinationPath $dest -Force
    Remove-Item $zip -Force
}

# JDK 17
$JdkDir = "$ToolsDir\jdk"
if (Test-Path "$JdkDir\bin\java.exe") {
    Ok "JDK 17 already present"
} else {
    Write-Host "  [1/4] Downloading JDK 17..." -ForegroundColor Yellow
    $jdkZip = "$tmp\jdk17.zip"
    Download "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.11%2B9/OpenJDK17U-jdk_x64_windows_hotspot_17.0.11_9.zip" $jdkZip
    Unzip $jdkZip "$tmp\jdk17_ext"
    $sub = Get-ChildItem "$tmp\jdk17_ext" -Directory | Select-Object -First 1
    if ($null -eq $sub) { Die "JDK extraction failed" }
    Move-Item $sub.FullName $JdkDir -Force
    Ok "JDK 17 installed"
}

# Gradle 8.7
$GradleDir = "$ToolsDir\gradle"
if (Test-Path "$GradleDir\bin\gradle.bat") {
    Ok "Gradle already present"
} else {
    Write-Host "  [2/4] Downloading Gradle 8.7..." -ForegroundColor Yellow
    $gradleZip = "$tmp\gradle.zip"
    Download "https://services.gradle.org/distributions/gradle-8.7-bin.zip" $gradleZip
    Unzip $gradleZip "$tmp\gradle_ext"
    $sub = Get-ChildItem "$tmp\gradle_ext" -Directory | Select-Object -First 1
    if ($null -eq $sub) { Die "Gradle extraction failed" }
    Move-Item $sub.FullName $GradleDir -Force
    Ok "Gradle 8.7 installed"
}

# Android cmdline-tools
$SdkDir     = "$ToolsDir\android-sdk"
$CtLatest   = "$SdkDir\cmdline-tools\latest"
$SdkManager = "$CtLatest\bin\sdkmanager.bat"
$env:JAVA_HOME = $JdkDir
$env:Path      = "$JdkDir\bin;$env:Path"

if (Test-Path $SdkManager) {
    Ok "Android cmdline-tools already present"
} else {
    Write-Host "  [3/4] Downloading Android cmdline-tools..." -ForegroundColor Yellow
    $ctZip = "$tmp\cmdline-tools.zip"
    Download "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip" $ctZip
    New-Item -ItemType Directory -Force -Path $CtLatest | Out-Null
    Unzip $ctZip "$tmp\ct_ext"
    $inner = "$tmp\ct_ext\cmdline-tools"
    if (-not (Test-Path $inner)) { Die "cmdline-tools extraction failed" }
    Get-ChildItem $inner | ForEach-Object { Move-Item $_.FullName $CtLatest -Force }
    Ok "Android cmdline-tools installed"
}

# SDK packages
Write-Host "  [4/4] Installing Android platform + build-tools..." -ForegroundColor Yellow
Log "Accepting licenses..."
("y`ny`ny`ny`ny`ny`ny`ny`ny`ny`n" | & "$SdkManager" --sdk_root="$SdkDir" --licenses) 2>&1 | Out-Null
Log "Installing platforms;android-34..."
& "$SdkManager" --sdk_root="$SdkDir" "platforms;android-34" | Out-Null
Log "Installing build-tools;34.0.0..."
& "$SdkManager" --sdk_root="$SdkDir" "build-tools;34.0.0" | Out-Null
Ok "Android SDK packages installed"

# apksigner.bat wrapper
$wrapperDir = "$ToolsDir\build-tools"
New-Item -ItemType Directory -Force -Path $wrapperDir | Out-Null
$apkSignerJar = "$SdkDir\build-tools\34.0.0\lib\apksigner.jar"
$bat = "@echo off`r`n`"$JdkDir\bin\java.exe`" -jar `"$apkSignerJar`" %*"
Set-Content -Path "$wrapperDir\apksigner.bat" -Value $bat -Encoding ASCII
Ok "apksigner.bat created"

# local.properties
$sdkPath = $SdkDir -replace '\\', '/'
Set-Content -Path "$InstallDir\local.properties" -Value "sdk.dir=$sdkPath" -Encoding UTF8
Ok "local.properties written"

# Cleanup
Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "  Setup complete!" -ForegroundColor Green
Write-Host "  Project: $InstallDir" -ForegroundColor White
Write-Host "  Open Html2Apk.sln in Visual Studio 2022 to build." -ForegroundColor White
Write-Host ""
