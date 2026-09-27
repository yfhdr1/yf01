cd C:\Users\yf_hdr\Desktop\yarekam\Assets
$scripts = Get-ChildItem .\Scripts -Filter *.cs -File
$map = @{}
foreach ($s in $scripts) { $m = Select-String -Path ($s.FullName + '.meta') -Pattern '^guid: (\w+)'; if ($m) { $map[$m.Matches[0].Groups[1].Value] = $s.BaseName } }
$files = Get-ChildItem -Recurse -File -Include *.unity,*.prefab
$hits = Select-String -Path $files.FullName -Pattern ($map.Keys -join '|') -AllMatches | ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
$cs = Get-ChildItem -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\Photon\\' }
foreach ($g in $map.Keys) { $n = $map[$g]; $scene = $hits -contains $g; $code = @($cs | Where-Object { $_.BaseName -ne $n } | Select-String -Pattern "\b$n\b" -List).Count; "{0,-30} Scene={1} Code={2}" -f $n, $scene, $code }
