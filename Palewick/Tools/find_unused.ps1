$ErrorActionPreference = 'SilentlyContinue'
$proj = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Join-Path $proj 'Assets'
$skip = '\\(Photon[^\\]*|TextMesh Pro|_TerrainAutoUpgrade|Plugins|PostProcessing)\\'
Write-Host 'Reading scenes and prefabs...'
$refFiles = Get-ChildItem $assets -Recurse -File -Include *.unity,*.prefab,*.asset,*.controller,*.overrideController,*.mat,*.playable
$used = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($f in $refFiles) {
    $fs = [IO.File]::OpenRead($f.FullName)
    $buf = New-Object byte[] 5
    [void]$fs.Read($buf, 0, 5)
    $fs.Close()
    if ([Text.Encoding]::ASCII.GetString($buf) -ne '%YAML') { continue }
    foreach ($m in [regex]::Matches([IO.File]::ReadAllText($f.FullName), 'guid: ([0-9a-f]{32})')) { [void]$used.Add($m.Groups[1].Value) }
}
Write-Host 'Checking scripts...'
$scripts = Get-ChildItem $assets -Recurse -File -Filter *.cs | Where-Object { $_.FullName -notmatch $skip }
$code = @{}
foreach ($s in $scripts) { $code[$s.FullName] = [IO.File]::ReadAllText($s.FullName) }
$lines = New-Object System.Collections.Generic.List[string]
foreach ($s in $scripts) {
    $rel = $s.FullName.Substring($proj.Length + 1)
    $guid = ''
    $meta = $s.FullName + '.meta'
    if (Test-Path $meta) { $guid = [regex]::Match([IO.File]::ReadAllText($meta), 'guid: ([0-9a-f]{32})').Groups[1].Value }
    $name = $s.BaseName
    $pattern = '\b' + [regex]::Escape($name) + '\b'
    $users = @()
    foreach ($k in $code.Keys) { if ($k -ne $s.FullName -and $code[$k] -match $pattern) { $users += (Split-Path $k -Leaf) } }
    if ($s.FullName -match '\\Editor\\') { $tag = 'EDITOR' }
    elseif ($guid -ne '' -and $used.Contains($guid)) { $tag = 'USED' }
    elseif ($code[$s.FullName] -match 'RuntimeInitializeOnLoadMethod') { $tag = 'BOOT' }
    elseif ($users.Count -gt 0) { $tag = 'CODE' }
    else { $tag = 'UNUSED' }
    $extra = ''
    if ($users.Count -gt 0) { $extra = ' | used by: ' + ($users -join ', ') }
    $lines.Add($tag + ' | ' + $rel + $extra)
}
$out = New-Object System.Collections.Generic.List[string]
$out.AddRange([string[]]($lines | Sort-Object))
$out.Add('')
$out.Add('FOLDER SIZES')
Get-ChildItem $assets -Directory | ForEach-Object {
    $sz = (Get-ChildItem $_.FullName -Recurse -File | Measure-Object Length -Sum).Sum
    $out.Add(('{0,8:N0} MB | {1}' -f ($sz / 1MB), $_.Name))
}
$report = Join-Path ([Environment]::GetFolderPath('Desktop')) 'unused_report.txt'
$out | Set-Content -Path $report -Encoding UTF8
$out -join "`r`n" | Set-Clipboard
Write-Host ('Done. Report copied to clipboard and saved to ' + $report)
