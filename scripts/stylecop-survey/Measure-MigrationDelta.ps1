#!/usr/bin/env pwsh
# Measures what switching from StyleCop to StyleBro changes in a StyleCop-clean repo, with the settings stylebro-migrate
# generates. The goal is (almost) nothing: every change is either a StyleBro bug, a migration gap, or an SDK rule that
# is broader than its StyleCop counterpart (docs/skipped-rules.md, "Covered, with known differences").
#
#   1. Plain 'dotnet format' without StyleBro, committed as the baseline (it has its own noise: line endings, conflict
#      markers in multi-targeted files).
#   2. stylebro-migrate --write, committed.
#   3. 'dotnet format' with StyleBro (and the preset unless the migration turned it off), StyleCop.Analyzers removed
#      like a real migration would. The remaining working-tree diff is StyleBro's.
#
# DESTRUCTIVE for the target: resets it to origin/HEAD and commits into it. Use a throwaway clone
# (git clone file:///C:/dev/<repo> survives the temp cleanup better than a hard-linked clone, see CLAUDE.md).
#
#   ./Measure-MigrationDelta.ps1 -Path <clone> -Solution Polly.slnx -ResetTargetRepo
#   then: git -C <clone> diff --ignore-cr-at-eol
param(
    [Parameter(Mandatory)][string]$Path,
    [Parameter(Mandatory)][string]$Solution,
    [switch]$ResetTargetRepo)
$ErrorActionPreference = 'Stop'
if (-not $ResetTargetRepo) { throw "This resets '$Path' to origin/HEAD and commits into it. Pass -ResetTargetRepo on a throwaway clone." }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

# Build the analyzers and the tool, then use copies of the DLLs so the run doesn't lock the build output.
dotnet build (Join-Path $repo 'src/StyleBro.CodeFixes') -nologo -v q | Out-Null
dotnet build (Join-Path $repo 'src/StyleBro.Migrate') -nologo -v q | Out-Null
$work = Join-Path ([IO.Path]::GetTempPath()) 'stylebro-delta'
New-Item -ItemType Directory -Force $work | Out-Null
Copy-Item (Join-Path $repo 'src/StyleBro.CodeFixes/bin/Debug/netstandard2.0/StyleBro.*.dll') $work -Force
Copy-Item (Join-Path $repo 'src/StyleBro.Package/build/stylebro.recommended.globalconfig') $work -Force
$hook = Join-Path $work 'delta-hook.targets'
Set-Content $hook @"
<Project>
  <ItemGroup>
    <Analyzer Include="$work\StyleBro.Analyzers.dll" />
    <Analyzer Include="$work\StyleBro.CodeFixes.dll" />
    <!-- EditorConfigFiles, not GlobalAnalyzerConfigFiles: from CustomAfterMicrosoftCommonTargets the latter is too late. -->
    <EditorConfigFiles Include="$work\stylebro.recommended.globalconfig" Condition="'`$(StyleBroPreset)' != 'none'" />
  </ItemGroup>
  <!-- A real migration removes StyleCop.Analyzers; its own fixes (SA1651 under dotnet format, which parses docs even
       where the build doesn't) aren't StyleBro's. -->
  <Target Name="StyleBroRemoveStyleCop" AfterTargets="ResolveLockFileAnalyzers">
    <ItemGroup>
      <Analyzer Remove="@(Analyzer)" Condition="`$([System.String]::Copy('%(Analyzer.Filename)').StartsWith('StyleCop'))" />
    </ItemGroup>
  </Target>
</Project>
"@

Push-Location $Path
try {
    $ErrorActionPreference = 'Continue'
    git reset -q --hard origin/HEAD; git clean -fdq 2>$null
    $env:CustomAfterMicrosoftCommonTargets = $null
    dotnet format $Solution --severity warn 2>&1 | Out-Null
    git add -A 2>$null; git commit -qm 'baseline: plain dotnet format' --no-verify 2>$null
    "Baseline: $(git log --oneline -1)"

    dotnet run --project (Join-Path $repo 'src/StyleBro.Migrate') --no-build -- . --write 2>&1 |
        Select-String '^(Read|StyleCop|Suppressions|Wrote|Kept|Turned)'
    git add -A 2>$null; git commit -qm 'stylebro-migrate' --no-verify 2>$null

    $env:CustomAfterMicrosoftCommonTargets = $hook
    dotnet format $Solution --severity warn 2>&1 | Out-Null
    "StyleBro changed $(@(git -c core.safecrlf=false status --short 2>$null).Count) files (including line-ending-only changes)"
    git -c core.safecrlf=false diff --ignore-cr-at-eol --stat 2>$null | Select-Object -Last 1
}
finally {
    $env:CustomAfterMicrosoftCommonTargets = $null
    Pop-Location
}
