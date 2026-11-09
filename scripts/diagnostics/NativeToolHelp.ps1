# Read only bounded, private help files. Text stays in memory; callers export metadata only.
function Read-NativeToolHelp([string]$Path) {
    $length = (Get-Item -LiteralPath $Path).Length
    if ($length -gt 1MB) { throw 'Native tool help exceeds its byte limit.' }
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -gt 1MB) { throw 'Native tool help exceeds its byte limit.' }
    $offset = 0
    $name = 'utf8-no-bom'
    $decoder = [Text.UTF8Encoding]::new($false, $true)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf) {
        $name = 'utf8-bom'; $offset = 3
    } elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xff -and $bytes[1] -eq 0xfe) {
        $name = 'utf16le-bom'; $offset = 2; $decoder = [Text.UnicodeEncoding]::new($false, $false, $true)
    } elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xfe -and $bytes[1] -eq 0xff) {
        $name = 'utf16be-bom'; $offset = 2; $decoder = [Text.UnicodeEncoding]::new($true, $false, $true)
    } elseif ($bytes -contains 0) {
        # Without a BOM, accept UTF-16 only when every pair unambiguously encodes
        # non-NUL single-byte characters on the same side. Otherwise fail closed.
        if ($bytes.Length % 2 -ne 0) { throw 'Unsupported or malformed native tool help encoding.' }
        $little = $bytes.Length -gt 0; $big = $little
        for ($index = 0; $index -lt $bytes.Length; $index += 2) {
            $little = $little -and $bytes[$index] -ne 0 -and $bytes[$index + 1] -eq 0
            $big = $big -and $bytes[$index] -eq 0 -and $bytes[$index + 1] -ne 0
        }
        if ($little -eq $big) { throw 'Unsupported or malformed native tool help encoding.' }
        if ($little) { $name = 'utf16le-no-bom'; $decoder = [Text.UnicodeEncoding]::new($false, $false, $true) }
        else { $name = 'utf16be-no-bom'; $decoder = [Text.UnicodeEncoding]::new($true, $false, $true) }
    }
    try { $text = $decoder.GetString($bytes, $offset, $bytes.Length - $offset) }
    catch { throw 'Unsupported or malformed native tool help encoding.' }
    if ($text.Contains([char]0)) { throw 'NUL characters in decoded native tool help.' }
    return @{ Text = $text; Encoding = $name; ByteCount = $bytes.Length }
}

function Get-NativeToolHelpCheck([string]$Tool, [string]$StdoutPath, [string]$StderrPath, [int]$ExitCode) {
    $metadata = [ordered]@{ HelpChecked = $false; HelpExitCode = $ExitCode; HelpStreams = @(); MissingSwitches = @(); HelpStatus = 'decode-failed' }
    $text = ''
    foreach ($stream in @(@{ Name = 'stdout'; Path = $StdoutPath }, @{ Name = 'stderr'; Path = $StderrPath })) {
        try {
            $decoded = Read-NativeToolHelp $stream.Path
            $metadata.HelpStreams += @{ Stream = $stream.Name; ByteCount = $decoded.ByteCount; Encoding = $decoded.Encoding }
            $text += $decoded.Text + "`n"
        } catch {
            $count = if (Test-Path -LiteralPath $stream.Path -PathType Leaf) { (Get-Item -LiteralPath $stream.Path).Length } else { $null }
            $metadata.HelpStreams += @{ Stream = $stream.Name; ByteCount = $count; Encoding = 'unsupported-or-over-limit' }
            return $metadata
        }
    }
    $required = if ($Tool -ceq 'procdump') { @('-e', '-b', '-f', '-n', '-mt', '-cancel') } elseif ($Tool -ceq 'cdb') { @('-z') } else { throw 'Unknown native tool help contract.' }
    # All original switches remain mandatory, regardless of the selected text encoding.
    $metadata.MissingSwitches = @($required | Where-Object { -not $text.Contains($_) })
    $metadata.HelpChecked = $metadata.MissingSwitches.Count -eq 0
    $metadata.HelpStatus = if ($metadata.HelpChecked) { 'checked' } else { 'missing-required-switches' }
    return $metadata
}
