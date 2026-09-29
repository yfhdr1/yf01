$ErrorActionPreference = 'SilentlyContinue'
$proj = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Join-Path $proj 'Assets'
$keepDir = '\\(Resources|StreamingAssets|Editor|Plugins|Gizmos|Photon[^\\]*|TextMesh Pro|_TerrainAutoUpgrade|Packages|ProjectSettings|UserSettings)\\'
$keepExt = @('.cs', '.dll', '.asmdef', '.asmref', '.shader', '.cginc', '.hlsl', '.compute', '.shadergraph', '.shadersubgraph', '.rsp', '.json', '.txt', '.md', '.xml', '.aar', '.jar', '.so')
function Test-Yaml($path) {
    $fs = [IO.File]::OpenRead($path)
    $buf = New-Object byte[] 5
    [void]$fs.Read($buf, 0, 5)
    $fs.Close()
    return ([Text.Encoding]::ASCII.GetString($buf) -eq '%YAML')
}
function Get-Refs($text) {
    $list = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($text, 'guid: ([0-9a-f]{32})')) { $list.Add($m.Groups[1].Value) }
    return $list
}
Write-Host 'Indexing assets...'
$guidToPath = @{}
$pathToGuid = @{}
$sizes = @{}
Get-ChildItem $assets -Recurse -File -Filter *.meta | ForEach-Object {
    $asset = $_.FullName.Substring(0, $_.FullName.Length - 5)
    if (Test-Path -LiteralPath $asset -PathType Leaf) {
        $g = [regex]::Match([IO.File]::ReadAllText($_.FullName), 'guid: ([0-9a-f]{32})').Groups[1].Value
        if ($g) {
            $guidToPath[$g] = $asset
            $pathToGuid[$asset] = $g
            $sizes[$asset] = (Get-Item -LiteralPath $asset).Length
        }
    }
}
$reached = New-Object 'System.Collections.Generic.HashSet[string]'
$queue = New-Object 'System.Collections.Generic.Queue[string]'
function Add-Path($p) {
    if ($p -and $reached.Add($p)) { $queue.Enqueue($p) }
}
Get-ChildItem (Join-Path $proj 'ProjectSettings') -File | ForEach-Object {
    if (Test-Yaml $_.FullName) {
        foreach ($g in (Get-Refs ([IO.File]::ReadAllText($_.FullName)))) { if ($guidToPath.ContainsKey($g)) { Add-Path $guidToPath[$g] } }
    }
}
$binaryAssets = New-Object System.Collections.Generic.List[string]
foreach ($p in @($pathToGuid.Keys)) {
    $ext = [IO.Path]::GetExtension($p).ToLower()
    if ($p -match $keepDir -or $keepExt -contains $ext) { Add-Path $p; continue }
    if ($ext -eq '.asset' -and -not (Test-Yaml $p)) { $binaryAssets.Add($p) }
}
Write-Host 'Following references...'
while ($queue.Count -gt 0) {
    $p = $queue.Dequeue()
    $texts = @()
    if (Test-Yaml $p) { $texts += [IO.File]::ReadAllText($p) }
    $texts += [IO.File]::ReadAllText($p + '.meta')
    foreach ($t in $texts) {
        foreach ($g in (Get-Refs $t)) { if ($guidToPath.ContainsKey($g)) { Add-Path $guidToPath[$g] } }
    }
}
Write-Host 'Building report...'
$dirTotal = @{}
$dirUnused = @{}
$dirBytes = @{}
$unusedFiles = New-Object System.Collections.Generic.List[string]
foreach ($p in $pathToGuid.Keys) {
    $isUnused = -not $reached.Contains($p)
    if ($isUnused) { $unusedFiles.Add($p) }
    $d = Split-Path $p -Parent
    while ($d.Length -gt $assets.Length) {
        $dirTotal[$d] = [int]$dirTotal[$d] + 1
        if ($isUnused) {
            $dirUnused[$d] = [int]$dirUnused[$d] + 1
            $dirBytes[$d] = [double]$dirBytes[$d] + $sizes[$p]
        }
        $d = Split-Path $d -Parent
    }
}
$full = @()
foreach ($d in $dirTotal.Keys) {
    if ($dirUnused[$d] -eq $dirTotal[$d]) {
        $parent = Split-Path $d -Parent
        if (-not ($parent.Length -gt $assets.Length -and $dirUnused[$parent] -eq $dirTotal[$parent])) { $full += $d }
    }
}
$out = New-Object System.Collections.Generic.List[string]
$totalUnused = 0
foreach ($p in $unusedFiles) { $totalUnused += $sizes[$p] }
$out.Add(('TOTAL UNUSED: {0:N0} MB in {1} files' -f ($totalUnused / 1MB), $unusedFiles.Count))
$out.Add('')
$out.Add('FULLY UNUSED FOLDERS')
foreach ($d in ($full | Sort-Object { - $dirBytes[$_] } | Select-Object -First 60)) {
    $out.Add(('{0,8:N1} MB | {1} files | {2}' -f ($dirBytes[$d] / 1MB), $dirTotal[$d], $d.Substring($proj.Length + 1)))
}
$out.Add('')
$out.Add('PARTLY USED FOLDERS (unused part)')
$partial = $dirTotal.Keys | Where-Object { $dirUnused[$_] -gt 0 -and $dirUnused[$_] -lt $dirTotal[$_] } | Sort-Object { - $dirBytes[$_] } | Select-Object -First 30
foreach ($d in $partial) {
    $out.Add(('{0,8:N1} MB | {1}/{2} unused | {3}' -f ($dirBytes[$d] / 1MB), $dirUnused[$d], $dirTotal[$d], $d.Substring($proj.Length + 1)))
}
$out.Add('')
$out.Add('BINARY .asset (kept, check)')
foreach ($p in $binaryAssets) { $out.Add(('{0,8:N1} MB | {1}' -f ($sizes[$p] / 1MB), $p.Substring($proj.Length + 1))) }
$out.Add('')
$out.Add('ODD FOLDERS INSIDE Assets')
foreach ($n in @('Packages', 'ProjectSettings', 'UserSettings', 'UI_Generated', 'Animations')) {
    $dp = Join-Path $assets $n
    if (Test-Path $dp) { $out.Add(('{0} | {1} files' -f $n, @(Get-ChildItem $dp -Recurse -File | Where-Object { $_.Extension -ne '.meta' }).Count)) }
}
$report = Join-Path ([Environment]::GetFolderPath('Desktop')) 'unused_assets.txt'
$out | Set-Content -Path $report -Encoding UTF8
$out -join "`r`n" | Set-Clipboard
Write-Host ('Done. Report copied to clipboard and saved to ' + $report)
