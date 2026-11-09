[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PrivateRoot,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$ManifestPath,
    [string]$ProcDumpPath,
    [switch]$Preflight,
    [string]$SyntheticPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Convert-SafeDebuggerOutput([string]$Text) {
    $frames = [Collections.Generic.List[string]]::new()
    $inStack = $false
    $exception = $null; $module = $null; $version = $null; $offset = $null
    $symbols = 'unavailable-or-deferred-private-symbols-not-configured'
    $exceptionAddress = $null; $moduleBase = $null; $moduleEnd = $null
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line -ceq 'NATIVE_STACK_BEGIN') { $inStack = $true; continue }
        if ($line -ceq 'NATIVE_STACK_END') { $inStack = $false; continue }
        # kc emits module/function only: never accept addresses, parameters, source paths or arbitrary text.
        if ($inStack -and $frames.Count -lt 32 -and $line -cmatch '^\s*[0-9a-fA-F]{1,2}\s+([A-Za-z0-9_.-]{1,128}(?:![A-Za-z0-9_?$@:.<>~`-]{1,256})?(?:\+0x[0-9a-fA-F]{1,16})?)\s*$') {
            $frames.Add($Matches[1])
        }
        if ($line -cmatch '^ExceptionCode:\s+([0-9a-fA-F]{8})\s*(?:\([^\r\n]*\))?\s*$') { $exception = '0x' + $Matches[1].ToUpperInvariant() }
        if ($line -cmatch '^\s*Image name:\s+([A-Za-z0-9_.-]{1,128})\s*$') { $module = $Matches[1] }
        if ($line -cmatch '^\s*File version:\s+([0-9.]{1,64})\s*$') { $version = $Matches[1] }
        if ($line -match '\(pdb symbols\)') { $symbols = 'pdb-loaded-public-server-private-symbols-not-configured' }
        if ($line -cmatch '^ExceptionAddress:\s+([0-9a-fA-F`]{1,17})(?:\s+.*)?$') { $exceptionAddress = [Convert]::ToUInt64($Matches[1].Replace('`', ''), 16) }
        if ($line -cmatch '^\s*([0-9a-fA-F`]{8,17})\s+([0-9a-fA-F`]{8,17})\s+[A-Za-z0-9_.-]{1,128}\s+.*$') {
            $moduleBase = [Convert]::ToUInt64($Matches[1].Replace('`', ''), 16)
            $moduleEnd = [Convert]::ToUInt64($Matches[2].Replace('`', ''), 16)
        }
    }
    if ($null -ne $exceptionAddress -and $null -ne $moduleBase -and $exceptionAddress -ge $moduleBase -and $exceptionAddress -lt $moduleEnd) {
        $offset = '0x' + ($exceptionAddress - $moduleBase).ToString('x')
    }
    [ordered]@{ ExceptionCode = $exception; ModuleBasename = $module; ModuleVersion = $version; FaultOffset = $offset
        Frames = @($frames.ToArray()); Symbols = $symbols
        InitialBreakpointExcluded = $false; Classification = 'inconclusive-first-chance-may-be-debugger-breakpoint' }
}
function Test-CaptureIdentity($Identity, $Processes) {
    $owners = @($Processes | Where-Object { [string]$_.Pid -ceq [string]$Identity.Pid -and
        [string]$_.CreationFileTime -ceq [string]$Identity.CreationFileTime -and
        $_.Basename -ceq 'msedgewebview2.exe' })
    return $owners.Count -eq 1
}
function Test-NativeBudget([int]$Count, [long]$Bytes, [double]$Seconds, [long]$CacheBytes) {
    return $Count -le 2 -and $Count -ge 0 -and $Bytes -le 256MB -and $Bytes -ge 0 -and
        $Seconds -lt 300 -and $Seconds -ge 0 -and $CacheBytes -le 512MB -and $CacheBytes -ge 0
}
function Test-PrivateDirectory([string]$Candidate, [string]$RunnerTemporaryRoot, [string]$AccountTemporaryRoot, [string]$ArtifactRoot) {
    try {
        $temporary = if ([string]::IsNullOrWhiteSpace($RunnerTemporaryRoot)) { $AccountTemporaryRoot } else { $RunnerTemporaryRoot }
        $temporary = [IO.Path]::GetFullPath($temporary).TrimEnd('\') + '\'
        $resolved = [IO.Path]::GetFullPath($Candidate).TrimEnd('\')
        $artifacts = [IO.Path]::GetFullPath($ArtifactRoot).TrimEnd('\')
        if (-not $resolved.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -or
            $resolved.Equals($artifacts, [StringComparison]::OrdinalIgnoreCase) -or
            $resolved.StartsWith($artifacts + '\', [StringComparison]::OrdinalIgnoreCase)) { return $false }
        # Check existing ancestors too: a syntactically contained path must not escape through a junction.
        $ancestor = $resolved
        while ($ancestor) {
            if (Test-Path -LiteralPath $ancestor) {
                $item = Get-Item -LiteralPath $ancestor -Force
                if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
            }
            $ancestor = [IO.Path]::GetDirectoryName($ancestor)
        }
        return $true
    } catch { return $false }
}
function Get-SignedTool([string]$Name, [string[]]$Candidates, [switch]$ExplicitPathOnly) {
    $command = if (-not $ExplicitPathOnly) { Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue } else { $null }
    $paths = @($Candidates)
    if ($command) { $paths = @($command.Source) + $paths }
    foreach ($path in $paths) {
        if (-not $path -or -not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
        $signature = Get-AuthenticodeSignature -LiteralPath $path
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { continue }
        return @{ Path = [IO.Path]::GetFullPath($path); Version = (Get-Item -LiteralPath $path).VersionInfo.FileVersion; Signature = 'Valid' }
    }
    return $null
}
function Get-PrivateBytes([string]$Root) {
    if (-not (Test-Path -LiteralPath $Root)) { return 0L }
    $sum = 0L
    foreach ($file in Get-ChildItem -LiteralPath $Root -File -Recurse) { $sum += $file.Length }
    return $sum
}
function Get-MinidumpIdentity([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = [IO.BinaryReader]::new($stream)
    try {
        if ($stream.Length -lt 32 -or $reader.ReadUInt32() -ne 0x504d444d) { return $null }
        $stream.Position = 8; $count = $reader.ReadUInt32(); $directory = $reader.ReadUInt32()
        if ($count -gt 4096 -or [long]$directory + 12L * $count -gt $stream.Length) { return $null }
        for ($index = 0; $index -lt $count; $index++) {
            $stream.Position = [long]$directory + 12L * $index
            $type = $reader.ReadUInt32(); $size = $reader.ReadUInt32(); $rva = $reader.ReadUInt32()
            if ($type -ne 15 -or $size -lt 24 -or [long]$rva + $size -gt $stream.Length) { continue }
            $stream.Position = $rva + 4; $flags = $reader.ReadUInt32()
            if (($flags -band 3) -ne 3) { return $null }
            return @{ Pid = $reader.ReadUInt32(); CreationUnixSeconds = $reader.ReadUInt32() }
        }
        return $null
    } finally { $reader.Dispose(); $stream.Dispose() }
}

# Synthetic input exercises pure policy/parser contracts without launching native tools.
if ($SyntheticPath) {
    $inputData = Get-Content -LiteralPath $SyntheticPath -Raw | ConvertFrom-Json
    $synthetic = @{ Parsed = (Convert-SafeDebuggerOutput $inputData.DebuggerText)
       IdentityAccepted = (Test-CaptureIdentity $inputData.Identity $inputData.Processes)
       BudgetAccepted = (Test-NativeBudget $inputData.Count $inputData.Bytes $inputData.Seconds $inputData.CacheBytes) }
    if ($inputData.PSObject.Properties.Name -contains 'DirectoryPolicy') {
        $policy = $inputData.DirectoryPolicy
        $synthetic.DirectoryAccepted = Test-PrivateDirectory $policy.Candidate $policy.RunnerTemp $policy.AccountTemp $policy.ArtifactRoot
    }
    $synthetic | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath
    return
}
$root = [IO.Path]::GetFullPath($PrivateRoot)
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../artifacts'))
if (-not (Test-PrivateDirectory $root $env:RUNNER_TEMP ([IO.Path]::GetTempPath()) $artifactRoot)) { throw 'Private root must be a non-linked child of runner temporary storage outside artifacts.' }
New-Item -ItemType Directory -Path $root -Force | Out-Null
$toolsPath = Join-Path $root 'verified-tools.json'
$result = [ordered]@{ Status = 'tools-unavailable'; Analyses = @(); RawExported = $false; HelpersStopped = $true; PrivateAnalysisRemoved = $false }
$kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/Debuggers/x64/cdb.exe'
$cdb = Get-SignedTool 'cdb.exe' @($kits)
if ($ProcDumpPath) {
    $explicitPath = [IO.Path]::GetFullPath($ProcDumpPath)
    if ($explicitPath -cne (Join-Path $root 'portable-tools/procdump64.exe') -or
        -not (Test-PrivateDirectory ([IO.Path]::GetDirectoryName($explicitPath)) $env:RUNNER_TEMP ([IO.Path]::GetTempPath()) $artifactRoot)) {
        throw 'Explicit portable ProcDump path must identify the private provisioned executable.'
    }
    $proc = Get-SignedTool 'procdump64.exe' @($explicitPath) -ExplicitPathOnly
} else {
    $proc = Get-SignedTool 'procdump64.exe' @('C:/Sysinternals/procdump64.exe', 'C:/Tools/Sysinternals/procdump64.exe',
    (Join-Path $env:ProgramData 'chocolatey/lib/sysinternals/tools/procdump64.exe'),
    (Join-Path $env:ProgramData 'chocolatey/lib/procdump/tools/procdump64.exe'))
if (-not $proc) { $proc = Get-SignedTool 'procdump.exe' @() }
}
$result['MissingTools'] = @($(if (-not $cdb) { 'cdb' }); $(if (-not $proc) { 'procdump' }))
if ($Preflight) {
    $result['Tools'] = @()
    try {
    if ($cdb -and $proc) {
        foreach ($tool in @($cdb, $proc)) {
            $name = if ($tool -eq $cdb) { 'cdb' } else { 'procdump' }
            $stdout = Join-Path $root "$name-help.txt"; $stderr = Join-Path $root "$name-help-error.txt"
            $helpArguments = if ($name -eq 'procdump' -and $ProcDumpPath) { '-accepteula -?' } else { '-?' }
            $helper = Start-Process -FilePath $tool.Path -ArgumentList $helpArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            try { if (-not $helper.WaitForExit(5000)) { $helper.Kill(); $helper.WaitForExit(); throw 'Tool help timed out.' } }
            finally { $helper.Dispose() }
            $help = (Get-Content -LiteralPath $stdout -Raw) + (Get-Content -LiteralPath $stderr -Raw)
            if ($name -eq 'procdump' -and (@('-e', '-b', '-f', '-n', '-mt', '-cancel') | Where-Object { -not $help.Contains($_) }).Count) { throw 'ProcDump breakpoint capture switches are unavailable.' }
            if ($name -eq 'cdb' -and -not $help.Contains('-z')) { throw 'CDB dump analysis switch unavailable.' }
            $result.Tools += @{ Name = $name; Version = $tool.Version; Signature = $tool.Signature; HelpChecked = $true }
        }
        @{ Cdb = $cdb.Path; ProcDump = $proc.Path } | ConvertTo-Json | Set-Content -LiteralPath $toolsPath
        $result.Status = 'ready'
    }
    } catch { $result.Status = 'tool-help-preflight-failed'; throw }
    finally { $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath }
    if ($result.Status -ne 'ready') { throw 'Signed CDB and ProcDump must already be installed; diagnostic stage stopped.' }
    return
}
if (-not $cdb -or -not $proc) { $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath; return }
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ([string]$manifest.SessionId -cnotmatch '^[0-9a-f]{32}$') { throw 'Invalid own-session identity.' }
$sessionRoot = Join-Path $root $manifest.SessionId
$activeRoot = Join-Path $sessionRoot 'active'
if (Test-Path -LiteralPath $activeRoot) {
    $result.HelpersStopped = $false
    $result['Capture'] = @{ Status = 'capture-cleanup-evidence-unavailable'; Attempted = $null; InitialBreakpointExcluded = $false }
    $captureStatusPath = Join-Path $activeRoot 'capture-status.json'
    if (Test-Path -LiteralPath $captureStatusPath) {
        try {
            $capture = Get-Content -LiteralPath $captureStatusPath -Raw | ConvertFrom-Json
            $allowedStatus = @('waiting-for-owned-process', 'private-size-limit', 'monitoring-first-chance-ambiguous',
                'identity-changed-or-exited-before-attach', 'attach-unavailable', 'watchdog-unavailable',
                'graceful-cancel-unavailable', 'forced-helper-stop-symptom-ambiguous')
            if ($capture.Status -cin $allowedStatus -and $capture.Attempted -ge 0 -and $capture.Attempted -le 2) {
                $result.Capture = @{ Status = [string]$capture.Status; Attempted = [int]$capture.Attempted; InitialBreakpointExcluded = $false }
                $result.HelpersStopped = $capture.HelpersStopped -ceq $true
            }
        } catch { $result.HelpersStopped = $false }
    }
}
if (-not $result.HelpersStopped) {
    $result.Status = 'capture-helper-cleanup-unconfirmed-no-analysis'
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath
    return
}
$analysisRoot = Join-Path $root 'offline-analysis'
$cache = Join-Path $analysisRoot 'symbols'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$clock = [Diagnostics.Stopwatch]::StartNew()
$analyses = [Collections.Generic.List[object]]::new()
try {
    $dumps = @(Get-ChildItem -LiteralPath $sessionRoot -Filter '*.dmp' -File -Recurse -ErrorAction SilentlyContinue)
    $result.Status = if ($dumps.Count) { 'inconclusive' } else { 'no-own-dump' }
    $selected = 0
    foreach ($dump in $dumps) {
        if (-not (Test-NativeBudget ($selected + 1) (Get-PrivateBytes $sessionRoot) $clock.Elapsed.TotalSeconds (Get-PrivateBytes $cache))) { $result.Status = 'budget-exhausted'; break }
        if ($dump.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        $identity = Get-MinidumpIdentity $dump.FullName
        if (-not $identity) { $analyses.Add(@{ Status = 'dump-identity-unavailable' }); continue }
        # MINIDUMP_MISC_INFO stores creation time in whole Unix seconds. Require a unique
        # natural own exit, fresh own-profile provenance, and expose the reduced precision.
        $owners = @($manifest.Processes | Where-Object {
            $_.Pid -eq $identity.Pid -and $_.Basename -ceq 'msedgewebview2.exe' -and $_.ExitFileTime -and -not $_.Cleanup -and
            [DateTimeOffset]::new([datetime]::FromFileTimeUtc([long]$_.CreationFileTime)).ToUnixTimeSeconds() -eq $identity.CreationUnixSeconds
        })
        if ($owners.Count -ne 1) { $analyses.Add(@{ Status = 'dump-identity-unmatched-or-ambiguous' }); continue }
        if ($owners[0].ExitCode -cne '0x80000003') {
            $analyses.Add(@{ Status = 'natural-exit-does-not-match-baseline-symptom' }); continue
        }
        if ($dump.Directory.Name -eq 'active') {
            $sidecar = [IO.Path]::ChangeExtension($dump.FullName, '.identity.json')
            if (-not (Test-Path -LiteralPath $sidecar) -or -not (Test-CaptureIdentity (Get-Content -LiteralPath $sidecar -Raw | ConvertFrom-Json) $owners)) {
                $analyses.Add(@{ Status = 'verified-attach-identity-missing-or-mismatched' }); continue
            }
        }
        $selected++
        $raw = Join-Path $analysisRoot "cdb-$selected.txt"; $err = Join-Path $analysisRoot "cdb-$selected-error.txt"
        # kc's count is hexadecimal: 20 means at most 32 frames, without argument values.
        $commands = '.exr -1; .ecxr; .echo NATIVE_STACK_BEGIN; kc 20; .echo NATIVE_STACK_END; lmv a @$ip; q'
        $args = '-z "' + $dump.FullName + '" -y "srv*' + $cache + '*https://msdl.microsoft.com/download/symbols" -c "' + $commands + '"'
        $helper = Start-Process -FilePath $cdb.Path -ArgumentList $args -WindowStyle Hidden -PassThru -RedirectStandardOutput $raw -RedirectStandardError $err
        try {
            while (-not $helper.WaitForExit(100)) {
                if (-not (Test-NativeBudget $selected (Get-PrivateBytes $sessionRoot) $clock.Elapsed.TotalSeconds (Get-PrivateBytes $cache)) -or (Get-PrivateBytes $analysisRoot) -gt 528MB) {
                    $helper.Kill(); $helper.WaitForExit(); $result.Status = 'budget-exhausted'; break
                }
            }
            if ($result.Status -eq 'budget-exhausted') { break }
            $parsed = Convert-SafeDebuggerOutput (Get-Content -LiteralPath $raw -Raw)
            $parsed['Provenance'] = @{ SessionId = $manifest.SessionId; Pid = $owners[0].Pid; CreationFileTime = $owners[0].CreationFileTime
                DumpIdentityPrecision = 'unix-seconds-unique-own-session-match'; NaturalExitCode = $owners[0].ExitCode
                Instrumentation = if ($dump.Directory.Name -eq 'active') { 'procdump-first-chance-ambiguous' } else { 'own-profile-crashpad' } }
            $parsed['Status'] = if ($helper.ExitCode -eq 0 -and $parsed.ExceptionCode -and $parsed.Frames.Count) { 'location-only-cause-unproven' } else { 'insufficient-debugger-output-or-symbols' }
            $analyses.Add($parsed)
        } finally { if (-not $helper.HasExited) { $helper.Kill(); $helper.WaitForExit() }; $helper.Dispose() }
    }
} finally {
    # Exact task-created analysis directory only; the workflow removes the complete private root.
    $resolvedAnalysis = [IO.Path]::GetFullPath($analysisRoot)
    if (-not $resolvedAnalysis.StartsWith($root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid analysis cleanup path.' }
    if (Test-Path -LiteralPath $resolvedAnalysis) { Remove-Item -LiteralPath $resolvedAnalysis -Recurse -Force }
    $result.PrivateAnalysisRemoved = -not (Test-Path -LiteralPath $resolvedAnalysis)
    $result.Analyses = @($analyses.ToArray())
    $result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath
}
