# Times dotnet format's fix loop per diagnostic id: for every fixed id, the analysis dotnet format runs before it, its
# Fix All, and the documents it changes (performance audit 2026-10, the "Fix loop per id" section of
# docs/proposals/performance-audit-2026-10.md).
#
#   ./scripts/benchmark/Measure-FixLoop.ps1 -Repo Newtonsoft [-Runs 2] [-Work <dir>] [-Only BRO1112,BRO1001] [-Properties NuGetAudit=false]
#   ./scripts/benchmark/Measure-FixLoop.ps1 -Url https://github.com/shouldly/shouldly -Commit b9b5293... -Solution Shouldly.sln
#
# -Repo takes a scripts/realworld/repos.psd1 entry; -Url/-Commit/-Solution any other repository. Like the real-world
# check (Invoke-RealWorld.ps1), StyleBro's Release DLLs go in through a CustomAfterMicrosoftCommonTargets hook, with every
# rule at its own default severity plus the newer presets' rules, no preset options; then each run is
#
#   dotnet format analyzers <sln> --diagnostics <every BRO id> --severity warn --no-restore -v diag
#
# on a clean checkout, with the FormatProbe startup hook (scripts/benchmark/FormatProbe) writing one line per analysis
# and per Fix All to <Work>/<name>-runN.probe.tsv and the console output to <name>-runN.log (time-format.py's format).
# Summarize with: python scripts/benchmark/summarize-fixloop.py <Work>/<name>-run*.probe.tsv
# Build StyleBro first: dotnet build src/StyleBro.CodeFixes -c Release -p:StyleBroSelf=none
param(
    [string]$Repo,
    [string]$Url,
    [string]$Commit,
    [string]$Solution,
    [string]$Name,
    [int]$Runs = 2,
    [string]$Work = (Join-Path ([IO.Path]::GetTempPath()) 'stylebro-fixloop'),
    [string[]]$Only = @(),
    # MSBuild properties for the restore, 'Name=Value' (Serilog: NuGetAudit=false).
    [string[]]$Properties = @()
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ($Repo) {
    $r = (Import-PowerShellDataFile (Join-Path $root 'scripts/realworld/repos.psd1'))[$Repo]
    if (-not $r) { throw "No repository '$Repo' in scripts/realworld/repos.psd1." }
    $Url, $Commit, $Solution = $r.Url, $r.Commit, $r.Solution
    if (-not $Name) { $Name = $Repo }
    if ($r.Env) { foreach ($key in $r.Env.Keys) { Set-Item "env:$key" $r.Env[$key] } }
}
if (-not $Name) { $Name = ($Url -split '/')[-1] }
New-Item -ItemType Directory -Force $Work | Out-Null

# The probe.
dotnet build (Join-Path $PSScriptRoot 'FormatProbe') -c Release -nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Building FormatProbe failed.' }
$probe = Join-Path $PSScriptRoot 'FormatProbe/bin/Release/net10.0/FormatProbe.dll'

# StyleBro, copied (a run must not lock the build output).
$bin = Join-Path $Work 'stylebro-bin'
New-Item -ItemType Directory -Force $bin | Out-Null
Copy-Item (Join-Path $root 'src/StyleBro.CodeFixes/bin/Release/netstandard2.0/StyleBro.*.dll') $bin -Force
$ids = @(Select-String -Path (Join-Path $root 'src/StyleBro.Analyzers/AnalyzerReleases.*.md') -Pattern '^(BRO\d{4})\s*\|' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)
if ($Only) { $ids = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ }) }
$enable = @(Get-ChildItem (Join-Path $root 'src/StyleBro.Package/build/presets') -Filter *.globalconfig |
    Where-Object Name -ne 'stylebro.1.0.globalconfig' | Get-Content |
    ForEach-Object { if ($_ -match '^dotnet_diagnostic\.(BRO\d{4})\.severity\s*=\s*(?!none)') { $Matches[1] } })
$enableConfig = Join-Path $Work 'stylebro-enable.globalconfig'
Set-Content $enableConfig (@('is_global = true') + @($enable | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" }))
$hook = Join-Path $Work 'stylebro-hook.targets'
Set-Content $hook @"
<Project>
  <ItemGroup>
    <Analyzer Include="$(Join-Path $bin 'StyleBro.Analyzers.dll')" />
    <Analyzer Include="$(Join-Path $bin 'StyleBro.CodeFixes.dll')" />
    <EditorConfigFiles Include="$enableConfig" />
  </ItemGroup>
  <PropertyGroup>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == '' and '`$(TargetFrameworks)' != ''">`$(TargetFrameworks.Replace(';', ','))</StyleBroTargetFrameworks>
    <StyleBroTargetFrameworks Condition="'`$(StyleBroTargetFrameworks)' == ''">`$(TargetFramework)</StyleBroTargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <CompilerVisibleProperty Include="StyleBroTargetFrameworks" />
    <CompilerVisibleProperty Include="LangVersion" />
    <CompilerVisibleProperty Include="MaxSupportedLangVersion" />
    <CompilerVisibleProperty Include="GenerateDocumentationFile" />
  </ItemGroup>
</Project>
"@

# The repository at its commit.
$path = Join-Path $Work $Name
if (-not (Test-Path (Join-Path $path '.git'))) {
    git init -q $path
    git -C $path remote add origin $Url
}
git -C $path cat-file -e "$Commit^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) { git -C $path fetch -q --depth 1 origin $Commit }
git -C $path checkout -q --force $Commit
if ($LASTEXITCODE -ne 0) { throw "Checking out $Commit failed in $path." }
git -C $path clean -fdxq
# global.json files asking for another SDK than the container's (Newtonsoft.Json's Src/global.json: 10.0.300).
function Remove-GlobalJson { Get-ChildItem $path -Recurse -Filter global.json -File | Remove-Item }
Remove-GlobalJson
$sln = Join-Path $path $Solution
$props = @($Properties | ForEach-Object { $_ -split ',' } | Where-Object { $_ } | ForEach-Object { "-p:$_" })

$env:CustomAfterMicrosoftCommonTargets = $hook
try {
    dotnet restore $sln -nologo -v q @props | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Restoring $sln failed." }
    for ($run = 1; $run -le $Runs; $run++) {
        git -C $path checkout -q --force .
        git -C $path clean -fdq -e bin -e obj
        Remove-GlobalJson
        $log = Join-Path $Work "$Name-run$run.log"
        $tsv = Join-Path $Work "$Name-run$run.probe.tsv"
        Remove-Item $tsv -ErrorAction SilentlyContinue
        $env:DOTNET_STARTUP_HOOKS = $probe
        $env:FORMAT_PROBE_LOG = $tsv
        try {
            python (Join-Path $PSScriptRoot 'time-format.py') $log -- dotnet format analyzers $sln --diagnostics @ids --severity warn --no-restore -v diag | Out-Host
        }
        finally { Remove-Item Env:\DOTNET_STARTUP_HOOKS, Env:\FORMAT_PROBE_LOG -ErrorAction SilentlyContinue }
        $changed = @(git -C $path diff --name-only).Count
        $hash = (git -C $path diff | Out-String | ForEach-Object { [BitConverter]::ToString([Security.Cryptography.MD5]::HashData([Text.Encoding]::UTF8.GetBytes($_))) })
        "run $run`: $changed files changed, diff md5 $hash" | Tee-Object -Append (Join-Path $Work "$Name-runs.txt") | Out-Host
    }
}
finally { Remove-Item Env:\CustomAfterMicrosoftCommonTargets -ErrorAction SilentlyContinue }
