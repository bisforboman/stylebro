#!/bin/bash
# run.sh <clone dir> <tag> [engine args...]: the trial steps on one clone; logs in logs/<tag>-*.log, summary in logs/<tag>-summary.tsv
set -u
D=$(realpath "$1"); TAG=$2; shift 2; ENG=("$@")
L=$(realpath "$(dirname "$0")")/logs
export NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1
S=$L/$TAG-summary.tsv
: > "$S"
cd "$D"
step() { # step <name> <cmd...>
  local name=$1; shift
  local t0=$(date +%s)
  "$@" > "$L/$TAG-$name.log" 2>&1
  local rc=$?
  local t=$(( $(date +%s) - t0 ))
  printf '%s\t%s\t%ss\t%s\n' "$name" "$rc" "$t" "$*" >> "$S"
  echo "$name rc=$rc ${t}s"
}
if [ "${SKIP_PREVIEW:-}" = "" ]; then
  step preview bro preview init "${ENG[@]}"
fi
# step 1 of getting-started: the package in Directory.Build.props
grep -q StyleBro.Analyzers Directory.Build.props || sed -i 's#<PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.0.0" PrivateAssets="All" />#&\n\t\t<PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50" PrivateAssets="all" />#' Directory.Build.props
step init bro init --write
step format bro format "${ENG[@]}"
step check bro check "${ENG[@]}"
bro check "${ENG[@]}" --json > "$L/$TAG-check.json" 2> "$L/$TAG-check-json.stderr"; echo -e "check-json\t$?" >> "$S"
step checkbuild bro check --build "${ENG[@]}"
bro check --build "${ENG[@]}" --json > "$L/$TAG-checkbuild.json" 2> "$L/$TAG-checkbuild-json.stderr"; echo -e "checkbuild-json\t$?" >> "$S"
for c in Debug Release; do
  step twae-$c dotnet build CommandLine.sln -c $c --no-incremental -p:NuGetAudit=false -p:TreatWarningsAsErrors=true
  step test-$c dotnet test tests/CommandLine.Tests -c $c -f netcoreapp3.1
done
git status --short > "$L/$TAG-status.txt"
git diff --stat > "$L/$TAG-diffstat.txt"
git diff > "$L/$TAG-diff.patch"
