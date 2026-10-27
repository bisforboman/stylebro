#!/usr/bin/env pwsh
# The fixture matrix (owner's choice, 2026-10-27: "fixture matrix first, then trials"): tiny repositories in the shapes
# that kept breaking first-run trials (scripts/fixtures/shapes/*.ps1, one per shape, each naming the trials it comes from),
# each run through the first-run path a user takes, with a locally packed StyleBro.Tool and StyleBro.Analyzers:
#   1. 'bro preview init' (or migrate) on the checkout, before anything references StyleBro;
#   2. the package reference (getting-started's step 1) and 'bro init --write' (or 'migrate --write'), committed;
#   3. on two clones of that commit, one per engine (the host, the default, and --engine dotnet-format):
#      'bro format', 'bro check', 'bro check --build', each with --json;
#   4. a TreatWarningsAsErrors build of the formatted code and the fixture's own tests.
# Every shape asserts that
#   - each verdict and exit code is honest: 0 only with "Clean." and nothing in notChecked, 3 only with "Not verified" and
#     the reason the shape expects, and the exit code the shape expects;
#   - format converges in one run (later runs change nothing, the check afterwards finds nothing to fix);
#   - the preview changes the same C# files (and baseline) as init + format, with the same text;
#   - the TWAE build and the tests pass;
#   - both engines give identical trees, exit codes and --json reports;
# plus the shape's own checks (Assert). Prints a table, writes it to $GITHUB_STEP_SUMMARY in CI, exits 1 on any failure.
#
#   pwsh scripts/fixtures/Invoke-Fixtures.ps1 [-Shape a,b] [-Shard i -Shards n] [-Work <folder>] [-Feed <folder> -Version <v>]
#        [-Engines host,dotnet-format] [-Keep]
#
# -Feed/-Version reuse packages packed earlier (both StyleBro packages and the helper packages in scripts/fixtures/packages);
# otherwise everything is packed with a version of its own (0.0.0-fixture.<time>), so the global packages folder never
# serves a stale build. -Work defaults to a new temp folder (OUTSIDE the repository: StyleBro's own props would leak in).
param(
    [string[]]$Shape = @(),
    [int]$Shard = 0,
    [int]$Shards = 1,
    [string]$Work = (Join-Path ([IO.Path]::GetTempPath()) "stylebro-fixtures-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"),
    [string]$Feed,
    [string]$Version,
    [string[]]$Engines = @('host', 'dotnet-format'),
    [switch]$Keep,
    [switch]$List)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Import-Module (Join-Path $PSScriptRoot 'Fixtures.psm1') -Force

# 'pwsh -File' passes '-Shape a,b' as one string.
$Shape = @($Shape | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$all = @(Get-ChildItem (Join-Path $PSScriptRoot 'shapes') -Filter '*.ps1' | Sort-Object Name)
if ($List) { $all | ForEach-Object { $_.BaseName }; exit 0 }
$chosen = @(for ($i = 0; $i -lt $all.Count; $i++) {
        if ($Shape.Count -gt 0 -and $all[$i].BaseName -notin $Shape) { continue }
        if ($Shape.Count -eq 0 -and $i % $Shards -ne $Shard) { continue }
        $all[$i]
    })
if ($Shape.Count -gt 0 -and $chosen.Count -ne $Shape.Count) { throw "Unknown shape among: $($Shape -join ', ') (-List lists them)." }

# Build tools that integrate with CI (Nerdbank.GitVersioning writes Git* variables to $GITHUB_ENV) must not reach the job's
# later steps, nor see a CI the fixture's user wouldn't have: the fixtures' commands don't get the step's files.
$summary = $env:GITHUB_STEP_SUMMARY
# Nor a CI at all: with GITHUB_ACTIONS set and no $GITHUB_ENV, Nerdbank.GitVersioning's build failed.
Remove-Item Env:GITHUB_ENV, Env:GITHUB_OUTPUT, Env:GITHUB_PATH, Env:GITHUB_STEP_SUMMARY, Env:GITHUB_ACTIONS, Env:CI -ErrorAction SilentlyContinue

$Work = [IO.Path]::GetFullPath($Work)
if ($Work.StartsWith($root + [IO.Path]::DirectorySeparatorChar)) { throw '-Work must be outside the repository (its props would leak into the fixtures).' }
New-Item -ItemType Directory -Force $Work | Out-Null

if (-not $Feed) {
    $Feed = Join-Path $Work 'feed'
    $Version = "0.0.0-fixture.$((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss'))"
    Write-Host "== Packing $Version into $Feed"
    foreach ($project in @((Join-Path $root 'src/StyleBro.Package'), (Join-Path $root 'src/StyleBro.Migrate')) + @(Get-ChildItem (Join-Path $PSScriptRoot 'packages') -Directory | ForEach-Object FullName)) {
        dotnet pack $project -c Release -o $Feed -p:Version=$Version --nologo -v q | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Packing $project failed." }
    }
}
elseif (-not $Version) { throw '-Feed needs -Version.' }

$Feed = [IO.Path]::GetFullPath($Feed)
$toolPath = Join-Path $Work 'tool'
if (-not (Test-Path (Join-Path $toolPath 'bro*'))) {
    dotnet tool install StyleBro.Tool --tool-path $toolPath --add-source $Feed --version $Version | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Installing the tool failed.' }
}

$bro = Join-Path $toolPath 'bro'
$context = [pscustomobject]@{ Version = $Version; Feed = $Feed; Work = $Work; Bro = $bro }
$results = [Collections.Generic.List[object]]::new()

function Run-Bro([string]$clone, [string]$logs, [string]$step, [string[]]$arguments) {
    $log = Join-Path $logs "$step.txt"
    $json = Join-Path $logs "$step.json"
    $watch = [Diagnostics.Stopwatch]::StartNew()
    Push-Location $clone
    try { & $bro @arguments --json 2> $log 1> $json }
    finally { Pop-Location }
    $code = $LASTEXITCODE
    $text = (Get-Content -Raw $log) + ''
    $report = $null
    try { $report = Get-Content -Raw $json | ConvertFrom-Json } catch { }
    Write-Host ("  {0,-28} exit {1} ({2:0} s)" -f $step, $code, $watch.Elapsed.TotalSeconds)
    return [pscustomobject]@{ Step = $step; Code = $code; Log = $log; Text = $text; Json = $json; Report = $report; Seconds = $watch.Elapsed.TotalSeconds }
}

# A --json report with sorted keys, without timings and output, the clone's path as <root>: what both engines must agree on.
function Canonical($result, [string]$clone) {
    if (-not (Test-Path $result.Json)) { return '' }
    $raw = ((Get-Content -Raw $result.Json) + '').Replace($clone, '<root>')
    if (-not $raw.Trim()) { return '' }
    return ($raw | python3 -c "import json, sys; r = json.load(sys.stdin); [r.pop(k, None) for k in ('timings', 'output')]; print(json.dumps(r, sort_keys=True, indent=1))") -join "`n"
}

# The working tree as git sees it: every changed or new file's content.
function Tree([string]$clone, [string]$base = 'HEAD') {
    git -C $clone add -A 2>$null | Out-Null
    $state = git -C $clone diff --cached --binary $base
    git -C $clone reset -q
    return ($state -join "`n")
}

# The verdict a command printed agrees with its exit code and with what the shape expects.
function Test-Verdict($result, [int]$expected, [string]$reason, $failures) {
    $name = $result.Step
    if ($result.Code -ne $expected) { $failures.Add("$name exited $($result.Code), expected $expected (log: $($result.Log))") }
    if ($result.Text -match '(?m)AD0001|Unhandled exception|System\.\w+Exception:') { $failures.Add("$name crashed or an analyzer threw (log: $($result.Log))") }
    $report = $result.Report
    switch ($result.Code) {
        0 {
            # check: "Clean." or "Clean: 1 finding kept on purpose"; format: "Run N: 0 files changed, clean."; the preview: "clean after N runs".
            if ($result.Text -notmatch '(?m)^Clean[.:]|, clean\.$|clean after \d+ runs?') { $failures.Add("$name exited 0 without saying it is clean") }
            # Notes (IDE0049 on an SDK that ignores the multi-target guard, ...) say the verdict doesn't count them.
            $gaps = @($report.notChecked | Where-Object { $_ -and $_ -notmatch "the verdict doesn't count" })
            if ($report -and ($report.clean -eq $false -or $gaps.Count -gt 0)) { $failures.Add("$name exited 0 but its --json has clean=$($report.clean), notChecked: $($gaps -join '; ')") }
        }
        3 {
            if ($result.Text -notmatch 'Not verified') { $failures.Add("$name exited 3 without saying 'Not verified'") }
            if ($result.Text -notmatch 'Not checked|Not compared|not verified') { $failures.Add("$name said Not verified without naming what wasn't checked") }
        }
    }

    if ($reason -and $result.Text -notmatch $reason) { $failures.Add("$name's output doesn't match /$reason/ (log: $($result.Log))") }
}

# The exit code and output pattern a step expects: the engine's own (EngineExpect/EngineReasons, keyed by engine) first.
function Expected($spec, [string]$step, [string]$engine = '') {
    $expect = if ($engine -and $spec.EngineExpect -and $spec.EngineExpect.Contains($engine)) { $spec.EngineExpect[$engine] } else { $spec.Expect }
    $reasons = if ($engine -and $spec.EngineReasons -and $spec.EngineReasons.Contains($engine)) { $spec.EngineReasons[$engine] } else { $spec.Reasons }
    $value = if ($expect -and $expect.Contains($step)) { $expect[$step] } else { 0 }
    $reason = if ($reasons -and $reasons.Contains($step)) { $reasons[$step] } else { $null }
    return $value, $reason
}

function Add-Reference([string]$repo, [string]$file = 'Directory.Build.props') {
    $path = Join-Path $repo $file
    $item = "  <ItemGroup>`n    <PackageReference Include=`"StyleBro.Analyzers`" Version=`"$Version`" PrivateAssets=`"all`" />`n  </ItemGroup>`n"
    if (Test-Path $path) { Set-Text $repo $file ((Get-Content -Raw $path).Replace('</Project>', "$item</Project>")) }
    else { Set-Text $repo $file "<Project>`n$item</Project>" }
}

foreach ($file in $chosen) {
    $name = $file.BaseName
    $spec = & $file.FullName
    $dir = Join-Path $Work $name
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    $logs = Join-Path $dir 'logs'
    New-Item -ItemType Directory -Force $logs | Out-Null
    $failures = [Collections.Generic.List[string]]::new()
    $watch = [Diagnostics.Stopwatch]::StartNew()
    Write-Host "== $name : $($spec.Description)"
    try {
        # The repository as its owner has it: the shape's files (and history), committed.
        $origin = Join-Path $dir 'origin'
        New-Item -ItemType Directory -Force $origin | Out-Null
        Set-Text $origin 'nuget.config' @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="fixtures" value="$Feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
        Set-Text $origin '.gitignore' "bin/`nobj/`n"
        & $spec.Create $origin $context
        Save-Commit $origin 'the fixture'

        # The user's checkout: a clone (shallow where the shape says so), maybe already built.
        $repo = Join-Path $dir 'repo'
        [string[]]$cloneArgs = if ($spec.Shallow) { @('--depth', '1', "file://$origin") } else { @($origin) }
        git clone -q @cloneArgs $repo
        if ($LASTEXITCODE -ne 0) { throw 'git clone failed.' }
        $base = (git -C $repo rev-parse HEAD).Trim()

        # The fixture itself builds (without warnings as errors), unless the shape says its build fails.
        if (-not $spec.BuildFails) {
            $untouched = Join-Path $dir 'untouched'
            git clone -q $repo $untouched
            $target = if ($spec.Target) { Join-Path $untouched $spec.Target } else { $untouched }
            dotnet build $target --nologo -v q @($spec.BuildArgs | Where-Object { $_ }) *> (Join-Path $logs 'untouched-build.txt')
            if ($LASTEXITCODE -ne 0) { throw "the untouched fixture doesn't build (log: $(Join-Path $logs 'untouched-build.txt'))" }
            Remove-Item -Recurse -Force $untouched
        }
        if ($spec.Before) { & $spec.Before $repo $context }

        # 1. The preview, before StyleBro is referenced.
        $command = if ($spec.Migrate) { 'migrate' } else { 'init' }
        $previewArgs = @('preview', $command) + @($spec.PreviewArgs | Where-Object { $_ })
        $preview = Run-Bro $repo $logs 'preview' $previewArgs
        $expected, $reason = Expected $spec 'preview'
        if (-not ($spec.Expect -and $spec.Expect.Contains('preview'))) { $expected, $reason = Expected $spec 'format' }
        Test-Verdict $preview $expected $reason $failures
        $patch = Join-Path $repo '.git/stylebro-preview.patch'

        # 2. Step 1 of getting-started and init --write; migrate --write and then the package swap (docs/migrating.md), committed.
        $reference = { if ($spec.Reference) { & $spec.Reference $repo $context } else { Add-Reference $repo } }
        if (-not $spec.Migrate) { & $reference }
        $init = Run-Bro $repo $logs $command @($command, '--write')
        if ($spec.Migrate) { & $reference }
        if ($init.Code -ne 0) { $failures.Add("$command --write exited $($init.Code) (log: $($init.Log))") }
        if ($spec.InitReason -and $init.Text -notmatch $spec.InitReason) { $failures.Add("$command's output doesn't match /$($spec.InitReason)/ (log: $($init.Log))") }
        Save-Commit $repo "stylebro $command"

        # 3. Format, check and check --build on one clone per engine.
        $runs = [ordered]@{}
        foreach ($engine in $Engines) {
            $clone = Join-Path $dir $engine
            git clone -q $repo $clone
            if ($spec.Before) { & $spec.Before $clone $context }
            $option = if ($engine -eq 'host') { @() } else { @('--engine', $engine) }
            $steps = [ordered]@{}
            foreach ($step in 'format', 'check', 'check-build') {
                [string[]]$arguments = switch ($step) { 'format' { @('format') } 'check' { @('check') } 'check-build' { @('check', '--build') } }
                $result = Run-Bro $clone $logs "$engine-$step" ($arguments + @($spec.Args | Where-Object { $_ }) + $option)
                $expected, $reason = Expected $spec $step $engine
                Test-Verdict $result $expected $reason $failures
                $steps[$step] = $result
                if ($step -eq 'format') { $steps['tree'] = Tree $clone }
            }

            # Converges in one run: no run after the first changed a file, and the check after it had nothing to fix.
            $later = @($steps['format'].Report.runs | Where-Object { $_.run -gt 1 -and @($_.filesChanged).Count -gt 0 })
            if ($later.Count -gt 0) { $failures.Add("$engine format changed files in run $($later[0].run): $(@($later[0].filesChanged) -join ', ')") }
            if ($steps['check'].Code -eq 2) { $failures.Add("$engine check after format still finds changes") }
            if ((Tree $clone) -ne $steps['tree']) { $failures.Add("$engine check or check --build changed the working tree") }
            $runs[$engine] = [pscustomobject]@{ Clone = $clone; Steps = $steps }
        }

        # Both engines: the same trees, exit codes and reports (unless the shape says why they differ: EnginesDiffer).
        if ($runs.Count -eq 2 -and -not $spec.EnginesDiffer) {
            $a, $b = @($runs.Keys)
            foreach ($step in 'format', 'check', 'check-build') {
                $x = $runs[$a].Steps[$step]; $y = $runs[$b].Steps[$step]
                if ($x.Code -ne $y.Code) { $failures.Add("$step exit codes differ: $a $($x.Code), $b $($y.Code)") }
                $cx = Canonical $x $runs[$a].Clone
                $cy = Canonical $y $runs[$b].Clone
                if ($cx -ne $cy) {
                    $lines = @(Compare-Object ($cx -split "`n") ($cy -split "`n") -SyncWindow 0 | Select-Object -First 12 | ForEach-Object { "      $(if ($_.SideIndicator -eq '<=') { $a } else { $b }): $($_.InputObject.Trim())" })
                    $failures.Add("$step --json differs between $a and $b ($($x.Json), $($y.Json)):`n$($lines -join "`n")")
                }
            }

            if ($runs[$a].Steps['tree'].Replace($runs[$a].Clone, '<root>') -ne $runs[$b].Steps['tree'].Replace($runs[$b].Clone, '<root>')) {
                $failures.Add("the working trees differ after format ($a vs $b)")
            }
        }

        $formatted = $runs[@($runs.Keys)[0]].Clone

        # The preview's patch, applied to the untouched checkout, gives the same C# files and baselines as init + format.
        if (Test-Path $patch) {
            $applied = Join-Path $dir 'preview-applied'
            git clone -q $repo $applied
            git -C $applied checkout -q $base 2>$null
            git -C $applied apply --whitespace=nowarn $patch 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { $failures.Add("the preview's patch doesn't apply to the checkout ($patch)") }
            else {
                $pattern = '(\.cs|stylebro\.baseline)$'
                $fromPreview = @(git -C $applied status --porcelain --untracked-files=all | ForEach-Object { $_.Substring(3) } | Where-Object { $_ -match $pattern } | Sort-Object)
                $fromFormat = @(git -C $formatted diff --name-only $base | Where-Object { $_ -match $pattern }) + @(git -C $formatted ls-files --others --exclude-standard | Where-Object { $_ -match $pattern }) | Sort-Object -Unique
                $diff = Compare-Object @($fromPreview | Where-Object { $_ }) @($fromFormat | Where-Object { $_ })
                if ($diff) { $failures.Add("the preview and format changed different files: $(($diff | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }) -join ', ')") }
                foreach ($path in $fromPreview | Where-Object { $_ -in $fromFormat }) {
                    $p = Join-Path $applied $path; $f = Join-Path $formatted $path
                    if ((Test-Path $p) -and (Test-Path $f) -and ((Get-FileHash $p).Hash -ne (Get-FileHash $f).Hash)) { $failures.Add("the preview and format wrote different text into $path") }
                }
            }
        }
        elseif ($preview.Code -in 0, 3) { $failures.Add('the preview wrote no patch') }

        # 4. The formatted code builds with warnings as errors and its tests pass.
        $target = if ($spec.Target) { Join-Path $formatted $spec.Target } else { $formatted }
        $buildLog = Join-Path $logs 'build.txt'
        dotnet build $target --nologo --no-incremental -p:TreatWarningsAsErrors=true @($spec.BuildArgs | Where-Object { $_ }) *> $buildLog
        $built = $LASTEXITCODE -eq 0
        if ($spec.BuildFails) {
            if ($built) { $failures.Add("the TWAE build passed, the shape expects it to fail ($($spec.BuildFails))") }
        }
        elseif (-not $built) { $failures.Add("the TWAE build of the formatted code failed (log: $buildLog)") }
        elseif ($spec.Tests -ne $false) {
            $testLog = Join-Path $logs 'test.txt'
            dotnet test $target --nologo --no-build @($spec.BuildArgs | Where-Object { $_ }) *> $testLog
            if ($LASTEXITCODE -ne 0) { $failures.Add("the tests failed after format (log: $testLog)") }
        }

        if ($spec.Assert) {
            foreach ($problem in @(& $spec.Assert $formatted $runs $context)) { if ($problem) { $failures.Add($problem) } }
        }
    }
    catch {
        $failures.Add("the runner failed: $_ ($($_.InvocationInfo.PositionMessage))")
    }

    $seconds = $watch.Elapsed.TotalSeconds
    $results.Add([pscustomobject]@{ Shape = $name; Ok = $failures.Count -eq 0; Seconds = $seconds; Failures = @($failures) })
    if ($failures.Count -eq 0) { Write-Host ("  PASS {0} ({1:0} s)" -f $name, $seconds) }
    else {
        Write-Host ("  FAIL {0} ({1:0} s)" -f $name, $seconds)
        $failures | ForEach-Object { Write-Host "    - $_" }
    }

    if (-not $Keep -and $failures.Count -eq 0) { Get-ChildItem $dir -Exclude 'logs' | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }
}

$table = @('| Shape | Result | Time |', '| --- | --- | --- |') + @($results | ForEach-Object {
        "| $($_.Shape) | $(if ($_.Ok) { 'pass' } else { 'FAIL: ' + (($_.Failures | ForEach-Object { $_.Replace('|', '\|') }) -join '<br>') }) | $('{0:0} s' -f $_.Seconds) |"
    })
$table | ForEach-Object { Write-Host $_ }
if ($summary) { Add-Content $summary (@("### Fixtures (shard $Shard of $Shards)", '') + $table) }
Write-Host "Work folder: $Work"
# Explicit both ways: without 'exit', a pwsh step ends with the last native command's exit code.
if (@($results | Where-Object { -not $_.Ok }).Count -gt 0) { exit 1 }
exit 0
