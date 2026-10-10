# Get-FailedTests for Invoke-RealWorld.ps1 (dot-sourced: uses its $r, $sln and $path). A file of its own because CI keys
# the cache of the untouched code's results on its hash (realworld.yml): a change here starts a new cache.
function Get-FailedTests {
    # Names of the failing tests. Not StyleBro's analyzers here: the tests run on the code as it is.
    $failed = [Collections.Generic.HashSet[string]]::new()
    if ($r.Tests -eq 'mtp') {
        dotnet build $sln -c Release -nologo -v q -p:TreatWarningsAsErrors=false 2>&1 | Out-Null
        $exes = Get-ChildItem (Join-Path $path 'artifacts/bin') -Recurse -Include '*Tests.exe', '*Specs.exe', '*Tests', '*Specs' -File |
            Where-Object { $_.BaseName -match '(Tests|Specs)$' -and $_.Directory.FullName -match '[\\/]release' -and ($_.Extension -in '.exe', '') }
        foreach ($exe in $exes) {
            & $exe.FullName 2>&1 | Select-String '^\s*failed (\S+)' | ForEach-Object { [void]$failed.Add("$($exe.Directory.Name)/$($_.Matches[0].Groups[1].Value)") }
        }
    }
    else {
        # TestFilter (repos.psd1): leaves out tests that depend on something other than the code, e.g. the network.
        $filter = if ($r.TestFilter) { @('--filter', $r.TestFilter) } else { @() }
        dotnet test $sln -nologo -p:TreatWarningsAsErrors=false @filter 2>&1 | Select-String '^\s+Failed (\S+)' |
            ForEach-Object { [void]$failed.Add($_.Matches[0].Groups[1].Value) }
    }
    # The comma keeps an empty set a set (PowerShell would unroll it to $null).
    return , $failed
}
