[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PrivateRoot,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$ArchivePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$archiveSha = '5FE68E81632F13188930AFFFF20A9A9910170BAE654360850D3108CEAD708A2D'
$binarySha = '22C2A2B09ADE73A342A18DAB838354385F73FC98E324B9903FD61FFF949C5124'
$root = [IO.Path]::GetFullPath($PrivateRoot).TrimEnd('\')
$runnerTemp = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$tempPrefix = [IO.Path]::GetFullPath($runnerTemp).TrimEnd('\') + '\'
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../artifacts')).TrimEnd('\')
if (-not $root.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    $root.Equals($artifactRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $root.StartsWith($artifactRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid portable tool private root.' }
$ancestor = $root
while ($ancestor) {
    if (Test-Path -LiteralPath $ancestor) {
        if (((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Linked portable tool storage is forbidden.' }
    }
    $ancestor = [IO.Path]::GetDirectoryName($ancestor)
}
$toolsRoot = Join-Path $root 'portable-tools'
if (Test-Path -LiteralPath $toolsRoot) { throw 'Portable tool storage must be fresh.' }
New-Item -ItemType Directory -Path $toolsRoot -Force | Out-Null
$archive = Join-Path $toolsRoot 'Procdump.zip'
$binary = Join-Path $toolsRoot 'procdump64.exe'
$result = [ordered]@{ Status = 'provisioning-failed'; Source = 'official-sysinternals-https'; ArchiveSha256 = $archiveSha
    BinarySha256 = $binarySha; Version = $null; Signature = $null; Executed = $false; TemporaryDownloadRemoved = $false }
$success = $false
try {
    if ($ArchivePath) {
        # Local offline verification is useful for reviewing the exact pinned package; no execution occurs.
        $inputFile = Get-Item -LiteralPath $ArchivePath
        if ($inputFile.Length -gt 8MB -or ($inputFile.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Invalid local archive.' }
        Copy-Item -LiteralPath $inputFile.FullName -Destination $archive
    } else {
        $handler = [Net.Http.HttpClientHandler]::new()
        $handler.AllowAutoRedirect = $false
        $handler.UseDefaultCredentials = $false
        $client = [Net.Http.HttpClient]::new($handler)
        $deadline = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(60))
        $response = $null; $source = $null; $target = $null
        try {
            $response = $client.GetAsync('https://download.sysinternals.com/files/Procdump.zip',
                [Net.Http.HttpCompletionOption]::ResponseHeadersRead, $deadline.Token).GetAwaiter().GetResult()
            if ([int]$response.StatusCode -ne 200 -or $response.Content.Headers.ContentLength -gt 8MB) { throw 'Official archive response rejected.' }
            $source = $response.Content.ReadAsStreamAsync($deadline.Token).GetAwaiter().GetResult()
            $target = [IO.File]::Create($archive)
            $buffer = [byte[]]::new(65536); $bytes = 0L
            while (($count = $source.ReadAsync($buffer, 0, $buffer.Length, $deadline.Token).GetAwaiter().GetResult()) -gt 0) {
                $bytes += $count
                if ($bytes -gt 8MB) { throw 'Official archive exceeds the download limit.' }
                $target.Write($buffer, 0, $count)
            }
        } finally {
            if ($target) { $target.Dispose() }; if ($source) { $source.Dispose() }; if ($response) { $response.Dispose() }
            $deadline.Dispose(); $client.Dispose(); $handler.Dispose()
        }
    }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -cne $archiveSha) { throw 'Pinned official archive hash mismatch.' }
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        # Select the exact root entry, never extract an archive-supplied directory or filename.
        $entries = @($zip.Entries | Where-Object { $_.Name -ieq 'procdump64.exe' })
        if ($entries.Count -ne 1 -or $entries[0].FullName -cne 'procdump64.exe' -or
            $entries[0].Length -le 0 -or $entries[0].Length -gt 16MB) { throw 'Unexpected executable archive entry.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $binary, $false)
    } finally { $zip.Dispose() }
    if ((Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -cne $binarySha) { throw 'Pinned executable hash mismatch.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $binary
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch '(?:^|,\s*)O=Microsoft Corporation(?:,|$)') { throw 'Portable ProcDump Microsoft signature is invalid.' }
    $version = (Get-Item -LiteralPath $binary).VersionInfo.FileVersion
    if ($version -cne '12.02') { throw 'Portable ProcDump version mismatch.' }
    $result.Status = 'ready-not-executed'; $result.Version = $version; $result.Signature = 'Valid'
    $success = $true
} finally {
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    $result.TemporaryDownloadRemoved = -not (Test-Path -LiteralPath $archive)
    if (-not $success -and (Test-Path -LiteralPath $toolsRoot)) {
        $checked = [IO.Path]::GetFullPath($toolsRoot)
        if (-not $checked.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid portable cleanup target.' }
        Remove-Item -LiteralPath $checked -Recurse -Force
    }
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath
}
