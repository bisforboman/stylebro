#!/bin/bash
# smoke.sh <clone> <solution>: restore + Debug build + tests (no network) for a random.json candidate; log in logs/smoke-<name>.log
D=$1; S=$2; L=/home/user/stylebro/.trials/trial55/logs/smoke-$(basename $D).log
export NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
cd $D
G=$(dirname $S)/global.json
if [ -f $G ] && grep -q '"version"' $G; then python3 -c "import json,sys;d=json.load(open('$G'));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open('$G','w'),indent=2)"; fi
T="$S"; [ -f $G ] && grep -q Microsoft.Testing.Platform $G && T="--solution $S"
{ echo "== build"; timeout 1200 dotnet build $S -c Debug -p:NuGetAudit=false 2>&1 | tail -15; echo "build rc ${PIPESTATUS[0]}"
  echo "== test"; timeout 1800 dotnet test $T -c Debug --no-build 2>&1 | grep -E 'Passed!|Failed!|error|Total|failed|succeeded|skipped' | tail -25; echo "test rc ${PIPESTATUS[0]}"; } > $L 2>&1
