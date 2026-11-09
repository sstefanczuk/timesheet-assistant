[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. "$PSScriptRoot/NativeToolHelp.ps1"
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('native-help-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    $stdout = Join-Path $temporary 'stdout.txt'; $stderr = Join-Path $temporary 'stderr.txt'
    [IO.File]::WriteAllBytes($stderr, [byte[]]@())
    $text = "PRIVATE-HELP-TEXT`r`nCapture Usage: -e 1 -b -f filter -n 1 -mt`r`nAutomated Termination: -cancel PID`r`n"
    $encodings = @(
        @{ Name = 'utf8-no-bom'; Value = [Text.UTF8Encoding]::new($false, $true) }
        @{ Name = 'utf8-bom'; Value = [Text.UTF8Encoding]::new($true, $true) }
        @{ Name = 'utf16le-no-bom'; Value = [Text.UnicodeEncoding]::new($false, $false, $true) }
        @{ Name = 'utf16le-bom'; Value = [Text.UnicodeEncoding]::new($false, $true, $true) }
        @{ Name = 'utf16be-no-bom'; Value = [Text.UnicodeEncoding]::new($true, $false, $true) }
        @{ Name = 'utf16be-bom'; Value = [Text.UnicodeEncoding]::new($true, $true, $true) }
    )
    foreach ($encoding in $encodings) {
        $bytes = [byte[]]($encoding.Value.GetPreamble() + $encoding.Value.GetBytes($text))
        [IO.File]::WriteAllBytes($stdout, $bytes)
        $result = Get-NativeToolHelpCheck 'procdump' $stdout $stderr 1
        if (-not $result.HelpChecked -or $result.HelpExitCode -ne 1 -or $result.HelpStreams[0].Encoding -cne $encoding.Name -or
            $result.HelpStreams[0].ByteCount -ne $bytes.Length) { throw "Valid help decoding failed: $($encoding.Name)" }
        $metadata = $result | ConvertTo-Json -Depth 6
        if ($metadata.Contains('PRIVATE-HELP-TEXT') -or $metadata.Contains('Capture Usage:')) { throw 'Raw help text escaped metadata.' }
    }
    foreach ($flag in @('-e', '-b', '-f', '-n', '-mt', '-cancel')) {
        [IO.File]::WriteAllBytes($stdout, [Text.Encoding]::UTF8.GetBytes($text.Replace($flag, 'REMOVED')))
        $result = Get-NativeToolHelpCheck 'procdump' $stdout $stderr 0
        if ($result.HelpChecked -or $result.MissingSwitches -cnotcontains $flag) { throw "Missing mandatory switch accepted: $flag" }
    }
    $malformed = @(
        [byte[]]@(0xc3, 0x28),
        [byte[]]@(0xff, 0xfe, 0x41),
        [byte[]]@(0xff, 0xfe, 0x00, 0xd8),
        [byte[]]@(0x41, 0, 0x42),
        [byte[]]@(0x41, 0, 0, 0x42)
    )
    foreach ($bytes in $malformed) {
        [IO.File]::WriteAllBytes($stdout, $bytes)
        $result = Get-NativeToolHelpCheck 'procdump' $stdout $stderr 0
        if ($result.HelpChecked -or $result.HelpStatus -cne 'decode-failed') { throw 'Malformed/ambiguous help encoding was accepted.' }
    }
    [IO.File]::WriteAllBytes($stdout, [byte[]]::new(1MB + 1))
    $result = Get-NativeToolHelpCheck 'procdump' $stdout $stderr 0
    if ($result.HelpChecked -or $result.HelpStreams[0].ByteCount -ne (1MB + 1)) { throw 'Oversized help escaped the bound.' }
    [IO.File]::WriteAllBytes($stdout, [Text.Encoding]::UTF8.GetBytes('Usage: cdb -z dump'))
    if (-not (Get-NativeToolHelpCheck 'cdb' $stdout $stderr 0).HelpChecked) { throw 'CDB dump switch validation failed.' }
    [IO.File]::WriteAllBytes($stdout, [Text.Encoding]::UTF8.GetBytes('Usage: cdb'))
    if ((Get-NativeToolHelpCheck 'cdb' $stdout $stderr 0).HelpChecked) { throw 'CDB missing switch was accepted.' }
    'Native tool help: 20 decoding/switch/bound cases passed; no native executable ran.'
} finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    $prefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid native help test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
