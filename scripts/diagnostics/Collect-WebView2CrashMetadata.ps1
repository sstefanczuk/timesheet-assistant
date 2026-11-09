[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$SyntheticEventsPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Convert-UtcTime($Value) {
    if ($Value -is [datetime]) { return $Value.ToUniversalTime() }
    if ($Value -is [datetimeoffset]) { return $Value.UtcDateTime }
    return [datetimeoffset]::Parse([string]$Value, [Globalization.CultureInfo]::InvariantCulture).UtcDateTime
}
function Convert-UnsignedValue([string]$Value) {
    if ($Value -match '^0x([0-9a-fA-F]+)$') { return [Convert]::ToUInt64($Matches[1], 16) }
    if ($Value -match '^[0-9]+$') { return [Convert]::ToUInt64($Value, 10) }
    return $null
}
function Get-Field($Fields, [string[]]$Names) {
    foreach ($name in $Names) { if ($Fields.Contains($name)) { return [string]$Fields[$name] } }
    return ''
}
function Get-SafeToken([string]$Value, [string]$Pattern) {
    if ($Value -cmatch $Pattern) { return $Value }
    return $null
}
function Get-ReportId($Fields) {
    $value = Get-Field $Fields @('IntegratorReportId', 'ReportId', 'ReportIdentifier')
    $guid = [guid]::Empty
    if ([guid]::TryParse($value, [ref]$guid) -and $guid -ne [guid]::Empty) { return $guid.ToString('D') }
    return $null
}
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$start = (Convert-UtcTime $manifest.SessionStartedUtc)
$end = (Convert-UtcTime $manifest.ObservedUtc)
$events = @()
$queryStatus = 'completed'
if ($SyntheticEventsPath) {
    $events = @(Get-Content -LiteralPath $SyntheticEventsPath -Raw | ConvertFrom-Json -AsHashtable)
} else {
    try {
        $records = @(Get-WinEvent -FilterHashtable @{ LogName = 'Application'; Id = @(1000, 1001); StartTime = $start; EndTime = $end } -ErrorAction Stop)
        $events = @(foreach ($record in $records) {
            # Never write or export XML, messages, System/Execution PID, or unknown fields.
            [xml]$xml = $record.ToXml()
            $fields = [ordered]@{}
            foreach ($item in $xml.Event.EventData.Data) { if ($item.Name) { $fields[[string]$item.Name] = [string]$item.'#text' } }
            @{ Id = $record.Id; Provider = $record.ProviderName; TimeUtc = $record.TimeCreated.ToUniversalTime().ToString('O'); Fields = $fields }
        })
    } catch {
        $queryStatus = if ($_.FullyQualifiedErrorId -like 'NoMatchingEventsFound*') { 'no-events' } else { 'query-unavailable' }
    }
}
$matched = [Collections.Generic.List[object]]::new()
$trustedReports = @{}
$unmatched = 0
foreach ($event in @($events | Sort-Object { [int]$_.Id })) {
    try {
        $time = (Convert-UtcTime $event.TimeUtc)
        if ($time -lt $start -or $time -gt $end) { $unmatched++; continue }
        $fields = $event.Fields
        $reportId = Get-ReportId $fields
        $key = $null
        if ([int]$event.Id -eq 1000 -and $event.Provider -eq 'Application Error') {
            $faultPid = Convert-UnsignedValue (Get-Field $fields @('ProcessId', 'FaultingProcessId'))
            $created = Convert-UnsignedValue (Get-Field $fields @('ProcessCreationTime', 'AppStartTime'))
            if ($null -eq $faultPid -or $null -eq $created) { $unmatched++; continue }
            $owners = @($manifest.Processes | Where-Object {
                [uint64]$_.Pid -eq $faultPid -and [uint64]$_.CreationFileTime -eq $created -and $_.ExitFileTime -and -not $_.Cleanup
            })
            if ($owners.Count -ne 1) { $unmatched++; continue }
            $key = "$($manifest.SessionId):${faultPid}:${created}"
            if ($reportId) {
                if ($trustedReports.ContainsKey($reportId) -and ($null -eq $trustedReports[$reportId] -or $trustedReports[$reportId] -ne $key)) { $trustedReports[$reportId] = $null }
                else { $trustedReports[$reportId] = $key }
            }
        } elseif ([int]$event.Id -eq 1001 -and $event.Provider -eq 'Windows Error Reporting' -and $reportId -and $trustedReports.ContainsKey($reportId)) {
            $key = $trustedReports[$reportId]
        }
        if (-not $key) { $unmatched++; continue }
        $module = Get-Field $fields @('ModuleName')
        # Path-like values are rejected instead of basename conversion: no user paths enter exports.
        $matched.Add([ordered]@{
            CorrelationKey = $key
            TimeUtc = $time.ToString('O')
            EventId = [int]$event.Id
            ReportId = $reportId
            ExceptionCode = Get-SafeToken (Get-Field $fields @('ExceptionCode')) '^(?:0x)?[0-9a-fA-F]{8}$'
            ModuleBasename = Get-SafeToken $module '^[a-zA-Z0-9_.-]{1,128}$'
            ModuleVersion = Get-SafeToken (Get-Field $fields @('ModuleVersion')) '^[0-9.]{1,64}$'
            FaultOffset = Get-SafeToken (Get-Field $fields @('FaultingOffset', 'FaultOffset')) '^(?:0x)?[0-9a-fA-F]{1,16}$'
        })
    } catch { $unmatched++ }
}
[ordered]@{
    SessionId = $manifest.SessionId
    QueryStatus = $queryStatus
    Status = if ($matched.Count) { 'matched' } else { 'no-unambiguous-match' }
    UnmatchedCount = $unmatched
    Events = @($matched.ToArray())
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding utf8
