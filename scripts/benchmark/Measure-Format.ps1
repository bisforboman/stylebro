<#
.SYNOPSIS
    Times `bro format`, `bro check` and `bro check --build` (with --timings) on one repository, next to a plain
    `dotnet format` and `dotnet build` of the same state. Used for the performance audit
    (docs/proposals/performance-audit-2026-10.md); the method follows docs/proposals/format-speed.md.

.DESCRIPTION
    Clones -Repository at -Commit into -Work/<name> (fresh each time), runs `bro init --write`, adds the StyleBro.Analyzers
    package (-Version, from -Feed) to the Directory.Build.props files init pinned the preset in (else the root one), restores and commits that as the starting state
    (a trial's step 4). Then, each step timed and its output kept in -Work/<name>-logs/:

      plain-format      plain `dotnet format <target>` on a copy of the starting state (then thrown away)
      format            `bro format <target> --timings`, then the result is committed (the clean state)
      check-record      `bro check <target> --timings` right after (answered from format's clean record when it can)
      check             the same with the clean records deleted (every check runs)
      check-build       `bro check <target> --build --timings`, records deleted
      plain-verify      `dotnet format <target> --verify-no-changes` on the clean state
      build             `dotnet build <target>` (incremental), then `--no-incremental`

    Extra MSBuild properties for every dotnet call (-Properties EnableWindowsTargeting=true) go through the
    environment, as 'Name=Value' strings. Prints a table of the steps at the end and writes it to -Work/<name>-logs/summary.tsv.

.EXAMPLE
    pwsh scripts/benchmark/Measure-Format.ps1 -Repository https://github.com/Tyrrrz/CliFx -Commit 10cbf9b6 -Feed ~/perf/feed -Version 0.0.1-audit
#>
param(
    [Parameter(Mandatory)] [string] $Repository,
    [Parameter(Mandatory)] [string] $Commit,
    [Parameter(Mandatory)] [string] $Feed,
    [Parameter(Mandatory)] [string] $Version,
    [string] $Target = '',
    [string] $Name = '',
    [string] $Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-format-speed'),
    [string[]] $Properties = @(),
    [string[]] $Skip = @()
)

$ErrorActionPreference = 'Stop'
if (-not $Name) { $Name = ($Repository.TrimEnd('/') -split '/')[-1] }
$path = Join-Path $Work $Name
$logs = Join-Path $Work "$Name-logs"
Remove-Item -Recurse -Force $path, $logs -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $logs | Out-Null
foreach ($property in $Properties) { $key, $value = $property -split '=', 2; Set-Item "env:$key" $value }

$steps = [Collections.Generic.List[object]]::new()
function Step([string] $step, [scriptblock] $block) {
    if ($Skip -contains $step) { return }
    $log = Join-Path $logs "$step.txt"
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & $block *> $log
    $code = $LASTEXITCODE
    $seconds = [Math]::Round($watch.Elapsed.TotalSeconds, 1)
    $steps.Add([pscustomobject]@{ Step = $step; Seconds = $seconds; Exit = $code })
    Write-Host ("{0,-14} {1,8:N1} s  exit {2}" -f $step, $seconds, $code)
}

function Clear-Records {
    # format's clean records (FormatCommand.SaveKept) and the probes it caches under the temp folder.
    Get-ChildItem ([IO.Path]::GetTempPath()) -Filter 'stylebro-kept*' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
}

git clone -q $Repository $path
git -C $path checkout -q $Commit
git -C $path -c user.name=audit -c user.email=noreply@example.com commit -q --allow-empty -m 'audit: start' | Out-Null
Push-Location $path
try {
    dotnet nuget add source $Feed --name stylebro-audit 2>$null | Out-Null
    Step 'init' { bro init --write }
    # The package goes where init pinned the preset (the props files the projects import, e.g. src/Directory.Build.props),
    # else into the root Directory.Build.props.
    $propsFiles = @(git ls-files -mo --exclude-standard '*Directory.Build.props' | Where-Object { (Get-Content $_ -Raw) -match 'StyleBroPreset' })
    if (-not $propsFiles) {
        $propsFiles = @('Directory.Build.props')
        if (-not (Test-Path 'Directory.Build.props')) { Set-Content 'Directory.Build.props' '<Project></Project>' }
    }
    $reference = @"
  <ItemGroup Condition="'`$(ManagePackageVersionsCentrally)' == 'true'">
    <PackageReference Include="StyleBro.Analyzers" VersionOverride="$Version" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup Condition="'`$(ManagePackageVersionsCentrally)' != 'true'">
    <PackageReference Include="StyleBro.Analyzers" Version="$Version" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@
    foreach ($props in $propsFiles) {
        $text = Get-Content $props -Raw
        $at = $text.LastIndexOf('</Project>')
        Set-Content $props ($text.Substring(0, $at) + $reference)
    }

    $targetArgs = @($Target | Where-Object { $_ })
    Step 'restore' { dotnet restore @targetArgs }
    git add -A; git -c user.name=audit -c user.email=noreply@example.com commit -q -m 'audit: step 4 (init --write + package)' | Out-Null

    Step 'plain-format' { dotnet format @targetArgs --no-restore }
    git checkout -q .; git clean -fdq -e bin -e obj

    Clear-Records
    Step 'format' { bro format @targetArgs --timings }
    git add -A; git -c user.name=audit -c user.email=noreply@example.com commit -q -m 'audit: formatted' | Out-Null
    Step 'check-record' { bro check @targetArgs --timings }
    Clear-Records
    Step 'check' { bro check @targetArgs --timings }
    Clear-Records
    Step 'check-build' { bro check @targetArgs --build --timings }
    Step 'plain-verify' { dotnet format @targetArgs --verify-no-changes --no-restore }
    Step 'build' { dotnet build @targetArgs --no-restore }
    Step 'build-full' { dotnet build @targetArgs --no-restore --no-incremental }
}
finally {
    dotnet nuget remove source stylebro-audit 2>$null | Out-Null
    Pop-Location
}

$steps | Format-Table -AutoSize | Out-Host
$steps | ForEach-Object { "$($_.Step)`t$($_.Seconds)`t$($_.Exit)" } | Set-Content (Join-Path $logs 'summary.tsv')
