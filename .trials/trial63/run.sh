#!/bin/bash
# run.sh <repo-key> <clone dir> <tag> [engine args...]: the trial steps on one clone.
# Per repository: .trials/trial63/<repo-key>/env.sh defines SLN (what the builds build), TESTS (a bash command run in the
# clone), SETUP (cmd: the getting-started step 1 edit, or the migrate command), PREVIEW (init or migrate).
# Logs in .trials/trial63/<repo-key>/logs/<tag>-*.log, summary in <tag>-summary.tsv.
set -u
KEY=$1; D=$(realpath "$2"); TAG=$3; shift 3; ENG=("$@")
R=/home/user/stylebro/.trials/trial63/$KEY
L=$R/logs; mkdir -p "$L"
export NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
. "$R/env.sh"
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
}
twae() { # twae <config>
  dotnet build $SLN -c $1 --no-incremental -p:NuGetAudit=false -p:TreatWarningsAsErrors=true
}
if [ "$TAG" = base ]; then
  step build dotnet build $SLN -c Debug
  for c in Debug Release; do step twae-$c twae $c; step test-$c env CFG=$c bash -c "$TESTS"; done
  echo DONE >> "$S"; exit 0
fi
if [ "${SKIP_PREVIEW:-}" = "" ]; then
  step preview bro preview $PREVIEW "${ENG[@]}"
fi
step setup bash -c "$SETUP"
[ -n "${PREBUILD_CHECK:-}" ] && step check-prebuild bro check ${FMT:-} "${ENG[@]}"
step format bro format ${FMT:-} "${ENG[@]}"
step check bro check ${FMT:-} "${ENG[@]}"
bro check ${FMT:-} "${ENG[@]}" --json > "$L/$TAG-check.json" 2> "$L/$TAG-check-json.stderr"; printf 'check-json\t%s\n' $? >> "$S"
python3 /home/user/stylebro/.trials/trial63/rmrecord.py "$D${FMT:+/$(dirname ${FMT:-.})}" > "$L/$TAG-rmrecord.txt"
step check-full bro check ${FMT:-} "${ENG[@]}"
python3 /home/user/stylebro/.trials/trial63/rmrecord.py "$D${FMT:+/$(dirname ${FMT:-.})}" >> "$L/$TAG-rmrecord.txt"
step checkbuild bro check ${FMT:-} --build "${ENG[@]}"
bro check ${FMT:-} --build "${ENG[@]}" --json > "$L/$TAG-checkbuild.json" 2> "$L/$TAG-checkbuild-json.stderr"; printf 'checkbuild-json\t%s\n' $? >> "$S"
for c in Debug Release; do step twae-$c twae $c; step test-$c env CFG=$c bash -c "$TESTS"; done
if [ -n "${PREBUILD_CHECK:-}" ]; then step check-afterbuild bro check ${FMT:-} "${ENG[@]}"; step checkbuild-afterbuild bro check ${FMT:-} --build "${ENG[@]}"; fi
git status --short > "$L/$TAG-status.txt"
git diff --stat > "$L/$TAG-diffstat.txt"
git diff > "$L/$TAG-diff.patch"
echo DONE >> "$S"
