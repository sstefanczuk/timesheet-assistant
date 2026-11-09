[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    $creation = '134000000000000000'
    $report = '34034bf4-3961-43cc-94af-f7b46f9baf11'
    @{ SessionId = 'synthetic'; SessionStartedUtc = '2026-11-08T10:00:00Z'; ObservedUtc = '2026-11-08T10:01:00Z'; Processes = @(
        @{ Pid = 42; CreationFileTime = $creation; ExitFileTime = '134000000000000100'; Cleanup = $false }
        @{ Pid = 44; CreationFileTime = $creation; ExitFileTime = '134000000000000100'; Cleanup = $false }
        @{ Pid = 45; CreationFileTime = $creation; ExitFileTime = '134000000000000100'; Cleanup = $true }
    ) } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $temporary 'manifest.json')
    function New-Event([int]$FaultPid, [string]$Created, [string]$Time = '2026-11-08T10:00:10Z') {
        @{ Id = 1000; Provider = 'Application Error'; TimeUtc = $Time; Fields = @{
            ProcessId = "$FaultPid"; ProcessCreationTime = $Created; IntegratorReportId = $report
            ModuleName = 'msedge.dll'; ModuleVersion = '1.2.3.4'; ExceptionCode = '80000003'; FaultingOffset = '00001234'
            AppPath = 'C:\Users\private\secret.exe'; Unexpected = 'DO-NOT-EXPORT'
        } }
    }
    $hex = New-Event 42 $creation
    $hex.Fields.Remove('ProcessId')
    $hex.Fields.Remove('ProcessCreationTime')
    $hex.Fields.FaultingProcessId = '0x2a'
    $hex.Fields.AppStartTime = '0x' + ([uint64]$creation).ToString('x')
    $pathModule = New-Event 42 $creation
    $pathModule.Fields.ModuleName = 'C:\Users\private\msedge.dll'
    $forged = New-Event 43 $creation
    $forged.SystemExecutionPID = 42
    $cases = @(
        @{ Name = 'cleanup exit excluded'; Event = (New-Event 45 $creation); Expected = 0 }
        @{ Name = 'hex fault PID and AppStartTime'; Event = $hex; Expected = 1 }
        @{ Name = 'module path redacted'; Event = $pathModule; Expected = 1; RedactedModule = $true }
        @{ Name = 'system execution PID ignored'; Event = $forged; Expected = 0 }
        @{ Name = 'correct identity'; Event = (New-Event 42 $creation); Expected = 1 }
        @{ Name = 'foreign PID'; Event = (New-Event 43 $creation); Expected = 0 }
        @{ Name = 'reused PID'; Event = (New-Event 42 '134000000000000001'); Expected = 0 }
        @{ Name = 'missing creation'; Event = (New-Event 42 ''); Expected = 0 }
        @{ Name = 'outside session'; Event = (New-Event 42 $creation '2026-11-08T09:59:59Z'); Expected = 0 }
        @{ Name = 'unlinked report'; Event = @{ Id = 1001; Provider = 'Windows Error Reporting'; TimeUtc = '2026-11-08T10:00:11Z'; Fields = @{ ReportId = $report } }; Expected = 0 }
        @{ Name = 'ambiguous report stays rejected'; Event = @((New-Event 42 $creation), (New-Event 44 $creation), (New-Event 42 $creation), @{ Id = 1001; Provider = 'Windows Error Reporting'; TimeUtc = '2026-11-08T10:00:11Z'; Fields = @{ ReportId = $report } }); Expected = 3 }
        @{ Name = 'linked report'; Event = @((New-Event 42 $creation), @{ Id = 1001; Provider = 'Windows Error Reporting'; TimeUtc = '2026-11-08T10:00:11Z'; Fields = @{ ReportId = $report } }); Expected = 2 }
    )
    foreach ($case in $cases) {
        ConvertTo-Json -InputObject @($case.Event) -Depth 6 | Set-Content (Join-Path $temporary 'events.json')
        & "$PSScriptRoot/Collect-WebView2CrashMetadata.ps1" -ManifestPath (Join-Path $temporary 'manifest.json') -SyntheticEventsPath (Join-Path $temporary 'events.json') -OutputPath (Join-Path $temporary 'result.json')
        $raw = Get-Content (Join-Path $temporary 'result.json') -Raw
        $result = $raw | ConvertFrom-Json
        if (@($result.Events).Count -ne $case.Expected) { throw "Failed correlation case: $($case.Name)" }
        if ($case.ContainsKey('RedactedModule') -and $result.Events[0].ModuleBasename) { throw 'Path-like module was not redacted.' }
        if ($raw.Contains('DO-NOT-EXPORT') -or $raw.Contains('private') -or $raw.Contains('AppPath')) { throw 'Unexpected field escaped the allowlist.' }
    }
    'Metadata correlation: 12 synthetic cases passed.'
} finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    if ($resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
