#!/usr/bin/env pwsh
# Runs every StyleBro rule on a public repository (scripts/realworld/repos.psd1) and checks what a user would see:
#   - no analyzer crashes (AD0001) and no new compile errors after 'dotnet format'
#   - no merge-conflict markers written into the source (multi-targeted projects)
#   - the fixes converge in one run: the second 'dotnet format' run changes no file (not '--verify-no-changes': it also
#     fails on warnings whose fix deliberately changes nothing, like the renames the string guards keep back); a repo
#     with a documented exception sets MaxRuns in repos.psd1
#   - with -Tests: no test that passed on the untouched code fails after the fixes
#   - Formatter = 'csharpier' (repos.psd1): a repository CSharpier formats, run the way its team would: 'bro
#     init --write' (which detects CSharpier and turns the layout rules off) and 'bro format' instead of every
#     rule; then the repository's own CSharpier ('dotnet tool restore', 'dotnet csharpier format .'), after which
#     'bro format --verify-no-changes' and 'dotnet csharpier check .' must be clean: the two settle
#   - BroFormat = $true (repos.psd1): after the checks above, the untouched code once more through 'bro init --write' and
#     'bro format' (its per-framework runs, whitespace pass and built-in rules, none of which the 'dotnet format analyzers'
#     runs above have), which must settle: exit 2 ("still changing after 3 runs") fails
#
#   ./scripts/realworld/Invoke-RealWorld.ps1 -Repo Serilog [-Tests] [-Work <dir>] [-Enable BRO1313,BRO1314] [-TestCache <file>]
# -Enable turns on rules that are off by default (they don't run otherwise).
# -TestCache: a JSON file with the tests that fail on the untouched code. Read when it exists (that build and test run is
# skipped), written otherwise. Those results depend only on the pinned commit, the SDK and FailedTests.ps1, not on
# StyleBro; CI keys the file on exactly those (realworld.yml).
# Every phase prints its duration, and the report lists them. The fix phase (the 'dotnet format' runs until no changes)
# is compared with the repo's MaxFixSeconds (repos.psd1): over it, a GitHub Actions warning (not a failure yet).
param(
    [Parameter(Mandatory)][string]$Repo,
    [string]$Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-realworld'),
    [switch]$Tests,
    [string[]]$Enable = @(),
    [string]$Configuration = 'Release',
    [string]$TestCache
)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$repos = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'repos.psd1')
if (-not $repos.ContainsKey($Repo)) { throw "Unknown repo '$Repo'. Known: $($repos.Keys -join ', ')" }
$r = $repos[$Repo]
# Env (repos.psd1): environment variables the repository's build needs here (Mediator: Nerdbank.GitVersioning can't compute
# a version in the shallow clone).
if ($r.Env) { foreach ($name in $r.Env.Keys) { Set-Item "env:$name" $r.Env[$name] } }

# Every rule in the release tracking files.
$ids = @(Select-String -Path (Join-Path $root 'src/StyleBro.Analyzers/AnalyzerReleases.*.md') -Pattern '^(BRO\d{4})\s*\|' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)

# StyleBro's analyzers, copied so the repository's builds don't lock this repository's build output.
$env:StyleBroSelf = 'none'
dotnet build (Join-Path $root 'src/StyleBro.CodeFixes') -c $Configuration -nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Building StyleBro failed.' }
$csharpier = $r.Formatter -eq 'csharpier'
$broFormat = [bool]$r.BroFormat
if ($csharpier -or $broFormat) {
    dotnet build (Join-Path $root 'src/StyleBro.Migrate') -c $Configuration -nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Building bro failed.' }
    # A copy, like the analyzers: a run in progress doesn't lock this repository's build output.
    $migrateBin = Join-Path $Work 'bro'
    Remove-Item -Recurse -Force $migrateBin -ErrorAction SilentlyContinue
    Copy-Item -Recurse (Join-Path $root "src/StyleBro.Migrate/bin/$Configuration/net8.0") $migrateBin
    $migrate = Join-Path $migrateBin 'StyleBro.Migrate.dll'

    # 'bro format' wants the package in the restore output, as a team would have it: packed under a version
    # of its own (a compiler server never sees an older build under the same path) and put straight into the global
    # packages folder, so the repository's nuget.config (packageSourceMapping) never has to know the local feed.
    $packageVersion = "0.0.0-realworld.$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"
    $feed = Join-Path $Work 'stylebro-feed'
    Remove-Item -Recurse -Force $feed -ErrorAction SilentlyContinue
    dotnet pack (Join-Path $root 'src/StyleBro.Package') -c $Configuration -o $feed -p:Version=$packageVersion -nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Packing StyleBro failed.' }
    $globalPackages = ((dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', '').Trim()
    $seed = Join-Path $Work 'stylebro-seed'
    New-Item -ItemType Directory -Force $seed | Out-Null
    Set-Content (Join-Path $seed 'nuget.config') "<configuration><packageSources><clear /><add key=`"local`" value=`"$feed`" /><add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`" /></packageSources></configuration>"
    Set-Content (Join-Path $seed 'Seed.csproj') "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><ImportDirectoryBuildProps>false</ImportDirectoryBuildProps><ImportDirectoryPackagesProps>false</ImportDirectoryPackagesProps></PropertyGroup><ItemGroup><PackageReference Include=`"StyleBro.Analyzers`" Version=`"$packageVersion`" /></ItemGroup></Project>"
    dotnet restore (Join-Path $seed 'Seed.csproj') --packages $globalPackages -nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Putting the StyleBro package into the global packages folder failed.' }
}
$bin = Join-Path $Work 'stylebro-bin'
New-Item -ItemType Directory -Force $bin | Out-Null
Copy-Item (Join-Path $root "src/StyleBro.CodeFixes/bin/$Configuration/netstandard2.0/StyleBro.*.dll") $bin -Force
$hook = Join-Path $Work 'stylebro-hook.targets'

# -Enable: rules that are off by default, turned on through a global config (EditorConfigFiles, not
# GlobalAnalyzerConfigFiles: the SDK has already converted those when this hook is imported).
$enableItem = ''
# 'pwsh Invoke-RealWorld.ps1 -Enable A,B' (from another shell, or CI) passes one string "A,B".
$Enable = @($Enable | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
# Rules of minors after 1.0 are off by default and on in the latest preset (docs/versioning.md): on here too.
$Enable += @(Get-ChildItem (Join-Path $root 'src/StyleBro.Package/build/presets') -Filter *.globalconfig |
    Where-Object Name -ne 'stylebro.1.0.globalconfig' | Get-Content |
    ForEach-Object { if ($_ -match '^dotnet_diagnostic\.(BRO\d{4})\.severity\s*=\s*(?!none)') { $Matches[1] } })
if ($Enable) {
    $enableConfig = Join-Path $Work 'stylebro-enable.globalconfig'
    Set-Content $enableConfig (@('is_global = true') + @($Enable | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }))
    $enableItem = "<EditorConfigFiles Include=`"$enableConfig`" />"
}
Set-Content $hook @"
<Project>
  <ItemGroup>
    <Analyzer Include="$(Join-Path $bin 'StyleBro.Analyzers.dll')" />
    <Analyzer Include="$(Join-Path $bin 'StyleBro.CodeFixes.dll')" />
    $enableItem
  </ItemGroup>
  <!-- What the package's build targets pass to the analyzers (multi-target guards: MultiTargetSuppressor, BRO1145, BRO1147). -->
  <PropertyGroup>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == '' and '`$(TargetFrameworks)' != ''">`$(TargetFrameworks.Replace(';', ','))</StyleBroTargetFrameworks>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == ''">`$(TargetFramework)</StyleBroTargetFrameworks>
  </PropertyGroup>
  <Target Name="_StyleBroFindSonar" BeforeTargets="GenerateMSBuildEditorConfigFileShouldRun">
    <ItemGroup>
      <_StyleBroSonarAnalyzer Include="@(Analyzer)" Condition="'%(Filename)' == 'SonarAnalyzer.CSharp'" />
    </ItemGroup>
    <PropertyGroup>
      <StyleBroSonar Condition="'`$(StyleBroSonar)' == ''">@(_StyleBroSonarAnalyzer->'%(FullPath)', '|')</StyleBroSonar>
    </PropertyGroup>
  </Target>
  <ItemGroup>
    <CompilerVisibleProperty Include="StyleBroSonar" />
    <CompilerVisibleProperty Include="StyleBroTargetFrameworks" />
    <CompilerVisibleProperty Include="LangVersion" />
    <CompilerVisibleProperty Include="MaxSupportedLangVersion" />
    <CompilerVisibleProperty Include="GenerateDocumentationFile" />
  </ItemGroup>
</Project>
"@
# The init flow (Formatter = 'csharpier', BroFormat): the package itself, as a team has it (analyzers, preset, build targets).
$packageHook = Join-Path $Work 'stylebro-package.targets'
if ($csharpier -or $broFormat) {
    Set-Content $packageHook @"
<Project>
  <ItemGroup Condition="'`$(ManagePackageVersionsCentrally)' == 'true'">
    <PackageReference Include="StyleBro.Analyzers" VersionOverride="$packageVersion" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup Condition="'`$(ManagePackageVersionsCentrally)' != 'true'">
    <PackageReference Include="StyleBro.Analyzers" Version="$packageVersion" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@
}
if ($csharpier) { $hook = $packageHook }

# The repository at its pinned commit.
$path = Join-Path $Work $Repo
if (-not (Test-Path (Join-Path $path '.git'))) {
    git init -q $path
    git -C $path remote add origin $r.Url
}
# CI caches the .git folder (realworld.yml): a clone that already has the pinned commit skips the fetch. Either way the
# working tree is reset to exactly that commit, with nothing left from a previous run.
git -C $path cat-file -e "$($r.Commit)^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) { git -C $path fetch -q --depth 1 origin $r.Commit }
git -C $path checkout -q --force $r.Commit
if ($LASTEXITCODE -ne 0) { throw "Checking out $($r.Commit) failed in $path." }
git -C $path clean -fdxq
$sln = Join-Path $path $r.Solution

# Every result below comes from 'git diff'; a broken clone must fail the run, not look like 'nothing changed'.
function Get-Diff([string[]]$options = @()) {
    $out = git -C $path diff @options
    if ($LASTEXITCODE -ne 0) { throw "git diff failed in $path (exit $LASTEXITCODE): the clone is broken." }
    $out
}

function Invoke-Build {
    $env:CustomAfterMicrosoftCommonTargets = $hook
    try { $out = dotnet build $sln -nologo --no-incremental -p:TreatWarningsAsErrors=false -p:CodeAnalysisTreatWarningsAsErrors=false 2>&1 }
    finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
    @{
        Errors = @($out | Where-Object { "$_" -match ': error (?!BRO|AD0001)' } | ForEach-Object { ("$_" -replace '\s*\[.*$', '').Replace($path, '') } | Sort-Object -Unique)
        Crashes = @($out | Where-Object { "$_" -match 'AD0001' } | ForEach-Object { ("$_" -replace '^.*AD0001: ', '') -replace '\s*\[.*$', '' } | Sort-Object -Unique)
        Findings = @($out | Where-Object { "$_" -match 'warning BRO\d{4}' } | ForEach-Object { ("$_" -replace '\s*\[.*$', '') } | Sort-Object -Unique).Count
    }
}

function Invoke-Format([switch]$Verify) {
    $env:CustomAfterMicrosoftCommonTargets = $hook
    try {
        if ($csharpier) {
            $migrateArgs = @('format', $sln)
            if ($Verify) { $migrateArgs += '--verify-no-changes' }
            dotnet $migrate @migrateArgs 2>&1 | Out-Host
            return $LASTEXITCODE
        }
        $formatArgs = @('format', 'analyzers', $sln, '--diagnostics') + $ids + @('--severity', 'warn', '--no-restore')
        if ($Verify) { $formatArgs += '--verify-no-changes' }
        dotnet @formatArgs 2>&1 | Out-Null
        return $LASTEXITCODE
    }
    finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
}

. (Join-Path $PSScriptRoot 'FailedTests.ps1')

# The files 'dotnet csharpier check .' rejects (run in the repository's folder), as paths relative to it. Not build output (bin/obj).
function Get-CSharpierRejected {
    @(dotnet csharpier check . 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -match '^Error (\S+) - Was not formatted' } |
        ForEach-Object { $Matches[1].Replace('\', '/') -replace '^\./', '' } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)/' } | Sort-Object -Unique)
}

# Runs a phase and records how long it took; its output is the block's output.
$timings = [Collections.Generic.List[string]]::new()
function Measure-Phase([string]$name, [scriptblock]$block) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try { & $block }
    finally {
        $timings.Add("$name $($watch.Elapsed.ToString('mm\:ss'))")
        Write-Host "[$($watch.Elapsed.ToString('mm\:ss'))] $name"
    }
}

$problems = [Collections.Generic.List[string]]::new()
$report = [Collections.Generic.List[string]]::new()
$report.Add("### $Repo")

$failedBefore = $null
if ($Tests -and $r.Tests) {
    if ($TestCache -and (Test-Path $TestCache)) {
        $failedBefore = [Collections.Generic.HashSet[string]]::new([string[]]@((Get-Content $TestCache -Raw | ConvertFrom-Json).Failed))
        $report.Add("- tests failing on the untouched code: $($failedBefore.Count) (cached)")
    }
    else {
        $failedBefore = Measure-Phase 'untouched code: Release build + tests' { Get-FailedTests }
        $report.Add("- tests failing on the untouched code: $($failedBefore.Count)")
        if ($TestCache) { @{ Repo = $Repo; Commit = $r.Commit; Failed = @($failedBefore | Sort-Object) } | ConvertTo-Json | Set-Content $TestCache }
        git -C $path clean -fdxq
    }
}

if ($csharpier) {
    Measure-Phase 'bro init --write' {
        dotnet $migrate init $path --write 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "bro init --write failed (exit $LASTEXITCODE)." }
        Push-Location $path
        try {
            # The repository's own CSharpier version, from its tool manifest.
            dotnet tool restore 2>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }
            # Files CSharpier rejects on the untouched code (StabilityMatrix: project files its hook doesn't format) don't count.
            $script:rejectedBefore = @(Get-CSharpierRejected)
        }
        finally { Pop-Location }
    }
}

$before = Measure-Phase 'untouched code: build with analyzers' { Invoke-Build }
$report.Add("- findings: $($before.Findings); compile errors before: $($before.Errors.Count)")
$before.Crashes | ForEach-Object { $problems.Add("analyzer crash: $_") }

$runs = 0
$converged = $false
$state = (Get-Diff | Out-String)
$fixWatch = [Diagnostics.Stopwatch]::new()
while ($runs -lt 3) {
    $runs++
    $fixWatch.Start()
    try { [void](Measure-Phase "dotnet format run $runs" { Invoke-Format }) }
    finally { $fixWatch.Stop() }
    $next = (Get-Diff | Out-String)
    if ($next -eq $state) { $converged = $true; break }
    $state = $next
}
$changed = @(Get-Diff '--name-only').Count
$allowed = if ($r.MaxRuns) { $r.MaxRuns } else { 2 }
$report.Add("- files changed: $changed; runs until no changes: $(if ($converged) { $runs } else { "more than $runs" }) (allowed: $allowed)")
if (-not $converged) { $problems.Add('the fixes did not converge in 3 runs') }
elseif ($runs -gt $allowed) { $problems.Add("$runs runs until no changes, allowed $allowed (one that changes, one clean; MaxRuns in repos.psd1 for a documented exception)") }

# Fix-time budget (the audit of 2026-10-22: a fix that re-scanned the solution per file went unnoticed, CI only timed the
# analyzers). Warn only for now; the next step, once the nightlies have shown each repo's median, is to make it a problem.
$fixSeconds = [int]$fixWatch.Elapsed.TotalSeconds
if ($r.MaxFixSeconds) {
    $report.Add("- fix time: $fixSeconds s in $runs runs (budget $($r.MaxFixSeconds) s, MaxFixSeconds)")
    if ($fixSeconds -gt $r.MaxFixSeconds) {
        $over = "the fixes took $fixSeconds s, over the budget of $($r.MaxFixSeconds) s (MaxFixSeconds in repos.psd1)"
        $report.Add("- **OVER BUDGET** (warning only): $over")
        if ($env:GITHUB_ACTIONS) { Write-Host "::warning title=Fix-time budget ($Repo)::$Repo`: $over" }
        else { Write-Warning "$Repo`: $over" }
    }
}
else { $report.Add("- fix time: $fixSeconds s in $runs runs (no budget set)") }

$markers = @(Get-Diff | Select-String '^\+(<<<<<<<|>>>>>>>) ')
if ($markers.Count) { $problems.Add("conflict markers written: $($markers.Count) lines") }

# CSharpier after StyleBro: how many files it rejects (StyleBro left layout it changes), then its format, after which
# StyleBro must find nothing more to change and CSharpier nothing more to format.
if ($csharpier) {
    Push-Location $path
    try {
        $rejected = @(Get-CSharpierRejected | Where-Object { $_ -notin $rejectedBefore })
        $report.Add("- files CSharpier rejects after StyleBro's format: $($rejected.Count)")
        $rejected | Select-Object -First 30 | ForEach-Object { Write-Host "  $_" }
        [void](Measure-Phase 'dotnet csharpier format .' { dotnet csharpier format . 2>&1 | Out-Null })
    }
    finally { Pop-Location }
    # '--verify-no-changes' must say clean (it formats a copy of the repository; the generator projects' build output goes
    # along, so Mapperly's in-solution generator runs there too).
    $verifyCode = 0
    [void](Measure-Phase 'bro format --verify-no-changes after CSharpier' { $script:verifyCode = Invoke-Format -Verify })
    if ($verifyCode -ne 0) { $problems.Add("after CSharpier, 'bro format --verify-no-changes' failed (exit $verifyCode)") }
    Push-Location $path
    try {
        $left = @(Get-CSharpierRejected)
        if ($left.Count) { $problems.Add("'dotnet csharpier check .' fails after CSharpier's own format: $($left.Count) files") }
    }
    finally { Pop-Location }
}

$after = Measure-Phase 'build after the fixes' { Invoke-Build }
$newErrors = @($after.Errors | Where-Object { $_ -notin $before.Errors })
$newErrors | ForEach-Object { $problems.Add("new compile error: $_") }
$after.Crashes | Where-Object { $_ -notin $before.Crashes } | ForEach-Object { $problems.Add("analyzer crash after fixing: $_") }
$report.Add("- warnings left (kept by guards): $($after.Findings); new compile errors: $($newErrors.Count)")

if ($null -ne $failedBefore) {
    $failedAfter = Measure-Phase 'tests after the fixes' { Get-FailedTests }
    # Known failures: documented limits (repos.psd1), e.g. a class a test serializes by reflection without attributes.
    $known = @($r.KnownFailures)
    $newFailures = @($failedAfter | Where-Object { -not $failedBefore.Contains($_) -and $_ -notin $known })
    $report.Add("- tests failing after the fixes: $($failedAfter.Count) (new: $($newFailures.Count))")
    $newFailures | ForEach-Object { $problems.Add("test fails after the fixes: $_") }
}

# 'bro format' as a team runs it (BroFormat): the analyzers-only runs above can't see a whitespace pass that each target
# framework's run does its own way (Newtonsoft.Json, 2026-10-23: '#endregion' in code net20 and net35 leave out flipped
# between their runs and the others', and 'bro format' never settled while this job passed).
if ($broFormat) {
    git -C $path checkout -q --force $r.Commit
    git -C $path clean -fdxq
    $env:CustomAfterMicrosoftCommonTargets = $packageHook
    try {
        Measure-Phase 'bro init --write (for bro format)' {
            dotnet $migrate init $path --write 2>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) { throw "bro init --write failed (exit $LASTEXITCODE)." }
        }
        $broCode = 0
        [void](Measure-Phase 'bro format' { dotnet $migrate format $sln 2>&1 | Out-Host; $script:broCode = $LASTEXITCODE })
    }
    finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
    $report.Add("- bro format after 'bro init --write': exit $broCode")
    # Not clean: what one more run changes, so the job shows the lines that keep flipping (the newest SDK applies fixes a
    # cloud container's SDK holds back, so they can't always be reproduced elsewhere).
    if ($broCode -eq 2) {
        git -C $path add -A
        $env:CustomAfterMicrosoftCommonTargets = $packageHook
        try { [void](Measure-Phase 'bro format --once (what keeps changing)' { dotnet $migrate format $sln --once 2>&1 | Out-Null }) }
        finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
        Write-Host "What one more 'bro format' run changes:"
        git -C $path diff | Select-Object -First 200 | Out-Host
    }
    # 3 is "Not verified" (a part no run could load or check), which says what it left out; 2 is "Not clean".
    if ($broCode -notin 0, 3) { $problems.Add("'bro format' after 'bro init --write' ended with exit $broCode (2: still changing after 3 runs)") }
}

$report.Add("- timings: $($timings -join '; ')")
$problems | ForEach-Object { $report.Add("- **PROBLEM**: $_") }
$report | Out-Host
if ($env:GITHUB_STEP_SUMMARY) { $report | Add-Content $env:GITHUB_STEP_SUMMARY }
if ($problems.Count) { throw "$Repo`: $($problems.Count) problem(s)." }
Write-Host "$Repo`: all good." -ForegroundColor Green
exit 0
