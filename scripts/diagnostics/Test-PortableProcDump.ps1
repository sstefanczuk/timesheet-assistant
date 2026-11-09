[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$temporaryBase = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$temporary = Join-Path $temporaryBase ('portable-procdump-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    # Neither rejected input can reach extraction, signature acceptance, help or debugger execution.
    $tampered = Join-Path $temporary 'tampered.zip'
    [IO.File]::WriteAllBytes($tampered, [byte[]]@(0x50, 0x4b, 0x03, 0x04, 0x00))
    $oversized = Join-Path $temporary 'oversized.zip'
    $file = [IO.File]::Create($oversized)
    try { $file.SetLength(8MB + 1) } finally { $file.Dispose() }
    $cases = @(
        @{ Name = 'pinned hash rejection'; Archive = $tampered; Expected = 'Pinned official archive hash mismatch.' }
        @{ Name = 'oversized local archive rejection'; Archive = $oversized; Expected = 'Invalid local archive.' }
    )
    foreach ($case in $cases) {
        $private = Join-Path $temporary ([guid]::NewGuid().ToString('N'))
        $output = Join-Path $temporary 'provision-status.json'
        $failure = $null
        try { & "$PSScriptRoot/Get-PortableProcDump.ps1" -PrivateRoot $private -OutputPath $output -ArchivePath $case.Archive }
        catch { $failure = $_.Exception.Message }
        if ($failure -cne $case.Expected) { throw "Unexpected portable provisioning result: $($case.Name)" }
        $status = Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
        if ($status.Executed -or $status.Status -cne 'provisioning-failed' -or -not $status.TemporaryDownloadRemoved -or
            (Test-Path -LiteralPath (Join-Path $private 'portable-tools'))) { throw 'Rejected package was retained or accepted.' }
    }
    'Portable ProcDump: 2 rejection/cleanup cases passed; no download or tool execution.'
} finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    $prefix = [IO.Path]::GetFullPath($temporaryBase).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid portable test cleanup target.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
