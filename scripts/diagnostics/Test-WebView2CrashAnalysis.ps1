[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('native-analysis-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    $text = "ExceptionCode: 80000003 (Break instruction exception)`nExceptionAddress: 0000000010001234 (msedge!SafeFunction)`n0000000010000000 0000000010100000 msedge (deferred)`nImage name: msedge.dll`nFile version: 1.2.3.4`nNATIVE_STACK_BEGIN`n"
    $text += "01 msedge!Unsafe(param=SECRET)`n02 C:\Users\private\secret.dll`n03 0000000012345678 msedge!Address`n"
    for ($i = 0; $i -lt 40; $i++) { $text += ('{0:x2} msedge!SafeFunction+0x1' -f $i) + "`n" }
    $text += "NATIVE_STACK_END`nDO-NOT-EXPORT`n"
    $base = @{ DebuggerText = $text; Identity = @{ Pid = 42; CreationFileTime = '134000000000000000' }
        Processes = @(@{ Pid = 42; CreationFileTime = '134000000000000000'; Basename = 'msedgewebview2.exe' })
        Count = 2; Bytes = 256MB; Seconds = 299; CacheBytes = 512MB }
    $cases = @(
        @{ Name = 'valid bounded report'; Identity = $true; Budget = $true }
        @{ Name = 'foreign PID'; PidValue = 43; Identity = $false; Budget = $true }
        @{ Name = 'reused PID'; Creation = '134000000000000001'; Identity = $false; Budget = $true }
        @{ Name = 'missing creation'; Creation = ''; Identity = $false; Budget = $true }
        @{ Name = 'PID limit'; Count = 3; Identity = $true; Budget = $false }
        @{ Name = 'byte limit'; Bytes = (256MB + 1); Identity = $true; Budget = $false }
        @{ Name = 'analysis deadline'; Seconds = 300; Identity = $true; Budget = $false }
        @{ Name = 'symbol cache limit'; CacheBytes = (512MB + 1); Identity = $true; Budget = $false }
    )
    foreach ($case in $cases) {
        $data = $base.Clone(); $data.Identity = $base.Identity.Clone()
        if ($case.ContainsKey('PidValue')) { $data.Identity.Pid = $case.PidValue }
        if ($case.ContainsKey('Creation')) { $data.Identity.CreationFileTime = $case.Creation }
        foreach ($key in @('Count', 'Bytes', 'Seconds', 'CacheBytes')) { if ($case.ContainsKey($key)) { $data[$key] = $case[$key] } }
        $inputPath = Join-Path $temporary 'input.json'; $outputPath = Join-Path $temporary 'output.json'
        $data | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $inputPath
        & "$PSScriptRoot/Analyze-WebView2Crash.ps1" -PrivateRoot $temporary -OutputPath $outputPath -SyntheticPath $inputPath
        $raw = Get-Content -LiteralPath $outputPath -Raw; $result = $raw | ConvertFrom-Json
        if ($result.IdentityAccepted -ne $case.Identity -or $result.BudgetAccepted -ne $case.Budget) { throw "Policy failure: $($case.Name)" }
        if (@($result.Parsed.Frames).Count -ne 32 -or $result.Parsed.ExceptionCode -ne '0x80000003' -or
            $result.Parsed.Frames[0] -ne 'msedge!SafeFunction+0x1' -or $result.Parsed.FaultOffset -ne '0x1234') { throw 'Bounded frame/module-offset parser failed.' }
        if ($raw.Contains('SECRET') -or $raw.Contains('DO-NOT-EXPORT') -or $raw.Contains('C:\') -or $raw.Contains('param=')) { throw 'Raw debugger data escaped.' }
        if ($result.Parsed.InitialBreakpointExcluded -or $result.Parsed.Classification -notmatch 'inconclusive') { throw 'Initial breakpoint was misclassified as causal.' }
    }
    foreach ($invalid in @(
        $text.Replace('ExceptionAddress: 0000000010001234 (msedge!SafeFunction)', 'ExceptionAddress: 0000000020001234 (outside!Function)'),
        $text.Replace('0000000010000000 0000000010100000 msedge (deferred)', 'no module metadata')
    )) {
        $data = $base.Clone(); $data.DebuggerText = $invalid
        $data | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $inputPath
        & "$PSScriptRoot/Analyze-WebView2Crash.ps1" -PrivateRoot $temporary -OutputPath $outputPath -SyntheticPath $inputPath
        $result = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json
        if ($null -ne $result.Parsed.FaultOffset) { throw 'Unknown or noncontaining module produced a fault offset.' }
    }
    # Hosted RUNNER_TEMP may be on a different volume from the account TEMP directory.
    $runnerTemp = Join-Path $temporary 'runner-temp'
    $accountTemp = Join-Path $temporary 'account-temp'
    $artifacts = Join-Path $runnerTemp 'artifacts'
    New-Item -ItemType Directory -Path $runnerTemp, $accountTemp | Out-Null
    $linkedDirectory = Join-Path $runnerTemp 'linked'
    New-Item -ItemType Junction -Path $linkedDirectory -Target $accountTemp | Out-Null
    $pathCases = @(
        @{ Name = 'runner child wins over account temp'; Candidate = (Join-Path $runnerTemp 'evidence'); Runner = $runnerTemp; Expected = $true }
        @{ Name = 'account child rejected with runner configured'; Candidate = (Join-Path $accountTemp 'evidence'); Runner = $runnerTemp; Expected = $false }
        @{ Name = 'account fallback'; Candidate = (Join-Path $accountTemp 'evidence'); Runner = ''; Expected = $true }
        @{ Name = 'temporary root itself'; Candidate = $runnerTemp; Runner = $runnerTemp; Expected = $false }
        @{ Name = 'temporary sibling prefix'; Candidate = ($runnerTemp + '-sibling/evidence'); Runner = $runnerTemp; Expected = $false }
        @{ Name = 'artifact root itself'; Candidate = $artifacts; Runner = $runnerTemp; Expected = $false }
        @{ Name = 'artifact descendant'; Candidate = (Join-Path $artifacts 'evidence'); Runner = $runnerTemp; Expected = $false }
        @{ Name = 'normalized parent escape'; Candidate = (Join-Path $runnerTemp '../escape'); Runner = $runnerTemp; Expected = $false }
        @{ Name = 'junction ancestor escape'; Candidate = (Join-Path $linkedDirectory 'evidence'); Runner = $runnerTemp; Expected = $false }
    )
    foreach ($case in $pathCases) {
        $data = $base.Clone()
        $data.DirectoryPolicy = @{ Candidate = $case.Candidate; RunnerTemp = $case.Runner; AccountTemp = $accountTemp; ArtifactRoot = $artifacts }
        $data | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $inputPath
        & "$PSScriptRoot/Analyze-WebView2Crash.ps1" -PrivateRoot $temporary -OutputPath $outputPath -SyntheticPath $inputPath
        $result = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json
        if ($result.DirectoryAccepted -ne $case.Expected) { throw "Private directory policy failure: $($case.Name)" }
    }
    'Native analysis: 19 policy/parser cases passed; no native debugger launched.'
} finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid test cleanup path.' }
    $junction = Join-Path $resolved 'runner-temp/linked'
    if (Test-Path -LiteralPath $junction) { Remove-Item -LiteralPath $junction -Force }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
