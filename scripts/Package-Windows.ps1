[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^0\.1\.0-preview\.[0-9]+$')][string]$Version,
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{40}$')][string]$Commit,
    [Parameter(Mandatory)][ValidateRange(1, 65535)][int]$BuildNumber,
    [string]$OutputDirectory = 'artifacts'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $repoRoot)
if (-not $outputRoot.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output directory must be inside the repository.'
}
$publish = Join-Path $outputRoot "publish-$Version"
$release = Join-Path $outputRoot "release-$Version"
if ((Test-Path -LiteralPath $publish) -or (Test-Path -LiteralPath $release)) {
    throw 'Output already exists. Use a fresh output directory; existing packages are never overwritten.'
}
New-Item -ItemType Directory -Path $publish, $release | Out-Null
dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj `
    -f net10.0-windows10.0.19041.0 -c Release `
    -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true -p:SelfContained=true `
    "-p:Version=$Version" "-p:ApplicationDisplayVersion=0.1.0" `
    "-p:ApplicationVersion=$BuildNumber" -o $publish
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }
if (-not (Test-Path -LiteralPath (Join-Path $publish 'TimesheetAssistant.exe'))) {
    throw 'Published executable is missing.'
}

@"
Timesheet Assistant $Version
Commit: $Commit

Technical preview of the starter; time entries and persistence are not implemented.
Target: Windows 11 x64. Unsigned application.
Extract the entire ZIP into its own directory and run TimesheetAssistant.exe.
Keep all files together. No SDK, Visual Studio, .NET or Windows App SDK installation
is intended to be required. Microsoft Edge WebView2 Evergreen Runtime is required:
https://developer.microsoft.com/microsoft-edge/webview2/

Clean-machine compatibility has not been verified. Developer-machine launch checks
are recorded separately in the deployment plan. SmartScreen or device policy may
warn about or block unsigned downloads. This preview has no automatic updater.
Home, Counter and Weather are sample pages, not product features.
"@ | Set-Content -LiteralPath (Join-Path $publish 'README.txt') -Encoding utf8

$zip = Join-Path $release "TimesheetAssistant-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip -CompressionLevel Optimal
if ((Get-Item -LiteralPath $zip).Length -ge 2GB) { throw 'Release ZIP must be smaller than 2 GiB.' }
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath "$zip.sha256" -Encoding ascii
@"
# Timesheet Assistant $Version

Technical prerelease of the Windows MAUI Blazor starter, not the functional MVP.

- Commit: $Commit
- Application download: TimesheetAssistant-$Version-win-x64.zip
- Windows 11 x64; unsigned; WebView2 Evergreen Runtime required.
- Extract the full ZIP and run TimesheetAssistant.exe; see README.txt inside.
- The automatic source archives are not runnable application downloads.
- No time-entry storage, cloud backend, updater or clean-machine verification.
- A successful build or empty xUnit run is not functional verification.
- First publication requires a recorded manual launch check on the developer PC.

SHA-256: $hash
"@ | Set-Content -LiteralPath (Join-Path $release 'release-notes.md') -Encoding utf8
Write-Host "Package: $zip"
Write-Host "SHA-256: $hash"
