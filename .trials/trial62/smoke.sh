#!/bin/bash
# Smoke build: restore + Debug build + tests of each trial 62 candidate, one at a time (untouched).
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
C=/home/user/cand62; L=/home/user/stylebro/.trials/trial62/logs/smoke; mkdir -p $L
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > $L/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' $L/$name.log) error lines"; }
for spec in "${@}"; do eval "$spec"; done
echo SMOKE-DONE
