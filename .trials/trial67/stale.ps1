Set-Location /root/wt/mine
. ./scripts/mutation/Mutations.Common.ps1
Get-ChildItem scripts/mutation/entries -Filter *.psd1 -Recurse | ForEach-Object {
    $m = Import-PowerShellDataFile $_.FullName
    $count = ([regex]::Matches([IO.File]::ReadAllText((Join-Path (Get-Location) $m.File)), [regex]::Escape($m.Find))).Count
    $rel = [IO.Path]::GetRelativePath((Join-Path (Get-Location) 'scripts/mutation/entries'), $_.FullName).Replace('\', '/')
    $area = Get-MutationArea $m.File
    if ($count -ne 1 -or -not $rel.StartsWith($area + '/')) { "$rel count=$count" }
}
