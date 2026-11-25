param(
    [ValidateSet("release", "dist")]
    [string]$Profile = "release",
    [string]$Version = "",
    [switch]$SyncReleaseDate,
    [switch]$UsePgo,
    [switch]$SkipEngine,
    [switch]$SkipApp
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$versionFile = Join-Path $repoRoot 'VERSION'
$engineDir = Join-Path $repoRoot 'engine'
$ruffleDir = Join-Path $engineDir 'ruffle'
$targetDir = Join-Path $engineDir 'target'
$distDir = Join-Path $repoRoot 'dist'
$distRuntime = Join-Path $distDir 'runtime'
$grimRelease = Join-Path $repoRoot 'grim\bin\x86\Release'

function Update-FileIfChanged {
    param([string]$Path, [string]$NewContent)
    if (-not (Test-Path $Path)) { return $false }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $oldContent = [System.IO.File]::ReadAllText($Path, $utf8NoBom)
    if ($oldContent -ceq $NewContent) { return $false }
    [System.IO.File]::WriteAllText($Path, $NewContent, $utf8NoBom)
    return $true
}

# 0. Resolve & sync single-source version across C#, Rust engine, PE metadata, and manifest
if ([string]::IsNullOrWhiteSpace($Version)) {
    if (-not (Test-Path $versionFile)) {
        throw "VERSION file not found at $versionFile. Pass -Version X.Y.Z or create VERSION."
    }
    $Version = (Get-Content $versionFile -Raw).Trim()
}
$cleanVer = $Version.Trim().TrimStart('v', 'V')
if ($cleanVer -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid version '$Version'. Expected semantic version X.Y.Z (e.g. 1.0.0)."
}
$assemblyVer = "$cleanVer.0"
$todayDate = (Get-Date).ToString('dd-MM-yyyy')

Write-Host "=== Syncing GrimCodex Ruffle version -> v$cleanVer ==="
$null = Update-FileIfChanged $versionFile "$cleanVer`n"

$assemblyInfoPath = Join-Path $repoRoot 'grim\Properties\AssemblyInfo.cs'
if (Test-Path $assemblyInfoPath) {
    $c = [System.IO.File]::ReadAllText($assemblyInfoPath)
    $c = $c -replace '\[assembly: AssemblyVersion\("[^"]+"\)', "[assembly: AssemblyVersion(`"$assemblyVer`")"
    $c = $c -replace '\[assembly: AssemblyFileVersion\("[^"]+"\)', "[assembly: AssemblyFileVersion(`"$assemblyVer`")"
    $null = Update-FileIfChanged $assemblyInfoPath $c
}

$codexMgrPath = Join-Path $repoRoot 'grim\Grimoire\Tools\CodexBankManager.cs'
if (Test-Path $codexMgrPath) {
    $c = [System.IO.File]::ReadAllText($codexMgrPath)
    $c = $c -replace 'public const string CURRENT_APP_VERSION = "[^"]+";', "public const string CURRENT_APP_VERSION = `"$cleanVer`";"
    $null = Update-FileIfChanged $codexMgrPath $c
}

$programCsPath = Join-Path $repoRoot 'grim\Grimoire\Program.cs'
if (Test-Path $programCsPath) {
    $c = [System.IO.File]::ReadAllText($programCsPath)
    $c = $c -replace 'public static readonly string Version = "[^"]+";', "public static readonly string Version = `"v$cleanVer`";"
    if ($SyncReleaseDate -or $PSBoundParameters.ContainsKey('Version')) {
        $c = $c -replace 'public static readonly string ReleaseDate = "[^"]+";', "public static readonly string ReleaseDate = `"$todayDate`";"
    }
    $null = Update-FileIfChanged $programCsPath $c
}

$grimcodexRsPath = Join-Path $ruffleDir 'desktop\src\grimcodex.rs'
if (Test-Path $grimcodexRsPath) {
    $c = [System.IO.File]::ReadAllText($grimcodexRsPath)
    $c = $c -replace 'pub const VERSION: &str = "[^"]+";', "pub const VERSION: &str = `"$cleanVer`";"
    $null = Update-FileIfChanged $grimcodexRsPath $c
}

$desktopCargoPath = Join-Path $ruffleDir 'desktop\Cargo.toml'
if (Test-Path $desktopCargoPath) {
    $c = [System.IO.File]::ReadAllText($desktopCargoPath)
    $c = $c -replace 'ProductVersion = "[^"]+"', "ProductVersion = `"$cleanVer`""
    $c = $c -replace 'FileVersion = "[^" ]+ \(([^)]+)\)"', "FileVersion = `"$cleanVer (`$1)`""
    $null = Update-FileIfChanged $desktopCargoPath $c
}

$manifestPath = Join-Path $repoRoot 'grim\CodexBank\manifest.json'
if (Test-Path $manifestPath) {
    $c = [System.IO.File]::ReadAllText($manifestPath)
    $c = $c -replace '"app_version":\s*"[^"]+"', "`"app_version`":  `"$cleanVer`""
    $null = Update-FileIfChanged $manifestPath $c
}

Get-Process -Name "GrimCodex Ruffle", "grimcodex-engine", "ruffle-engine" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 1. Build Ruffle runtime engine (x64 Rust) -> runtime\grimcodex-engine.exe
if (-not $SkipEngine) {
    if (-not (Test-Path $ruffleDir)) {
        throw "Engine source not found at $ruffleDir."
    }

    $rustVersion = (Get-Content (Join-Path $engineDir 'RUST_VERSION') -Raw).Trim()

    $portableJdk = Join-Path $env:LOCALAPPDATA 'Programs\temurin-21'
    if (Test-Path (Join-Path $portableJdk 'bin\javac.exe')) {
        $env:JAVA_HOME = $portableJdk
        $env:PATH = "$(Join-Path $portableJdk 'bin');$env:PATH"
    }

    $flags = @(
        '-Ctarget-feature=+crt-static',
        '-Ctarget-cpu=x86-64',
        "--remap-path-prefix=$ruffleDir=ruffle",
        "--remap-path-prefix=$env:USERPROFILE=home"
    )
    if ($env:CARGO_HOME) { $flags += "--remap-path-prefix=$env:CARGO_HOME=cargo" }
    if ($UsePgo) {
        $pgoOut = Join-Path $engineDir 'pgo-extracted'
        Expand-Archive -Force (Join-Path $engineDir 'pgo\aqw.profdata.zip') $pgoOut
        $flags += "-Cprofile-use=$(Join-Path $pgoOut 'aqw.profdata')"
    }

    Remove-Item Env:RUSTFLAGS -ErrorAction SilentlyContinue
    $env:CARGO_ENCODED_RUSTFLAGS = $flags -join ([char]0x1f)
    $env:CARGO_TARGET_DIR = $targetDir

    Write-Host "=== Building GrimCodex Ruffle Engine v$cleanVer (toolchain=$rustVersion, profile=$Profile) ==="
    Push-Location $ruffleDir
    try {
        & cargo "+$rustVersion" build --locked --package ruffle_desktop --profile $Profile
        if ($LASTEXITCODE -ne 0) { throw "cargo build failed ($LASTEXITCODE)" }
    } finally {
        Pop-Location
    }
}

$builtEngine = Join-Path $targetDir "$Profile\ruffle_desktop.exe"
if (-not (Test-Path $builtEngine)) {
    throw "Built engine binary not found at $builtEngine."
}

# 2. Build GrimCodex Ruffle WinForms app (x86 .NET 4.7.2)
if (-not $SkipApp) {
    Write-Host "=== Building GrimCodex Ruffle.exe v$cleanVer ==="
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'grim\build.ps1') -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw "grim build failed ($LASTEXITCODE)" }
}

# 3. Assemble unified 1-folder distribution in dist\
Write-Host "=== Assembling unified app in $distDir ==="
New-Item -ItemType Directory -Force $distDir, $distRuntime, (Join-Path $distDir 'Loader'), (Join-Path $distDir 'Resources') | Out-Null

Copy-Item -Force $builtEngine (Join-Path $distRuntime 'grimcodex-engine.exe')
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $distRuntime 'ruffle-engine.exe')
New-Item -ItemType Directory -Force (Join-Path $grimRelease 'runtime') | Out-Null
Copy-Item -Force $builtEngine (Join-Path $grimRelease 'runtime\grimcodex-engine.exe')
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $grimRelease 'runtime\ruffle-engine.exe')

$appFiles = @(
    'GrimCodex Ruffle.exe',
    'GrimCodex Ruffle.exe.config',
    'ClientConfig.cfg',
    'config.cfg'
)
foreach ($f in $appFiles) {
    $src = Join-Path $grimRelease $f
    if (Test-Path $src) { Copy-Item -Force $src (Join-Path $distDir $f) }
}
Copy-Item -Force (Join-Path $grimRelease 'Loader\*') (Join-Path $distDir 'Loader') -Recurse -ErrorAction SilentlyContinue
Copy-Item -Force (Join-Path $grimRelease 'Resources\*') (Join-Path $distDir 'Resources') -Recurse -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Build complete (v$cleanVer)! Run:"
Write-Host "  $(Join-Path $distDir 'GrimCodex Ruffle.exe')"
