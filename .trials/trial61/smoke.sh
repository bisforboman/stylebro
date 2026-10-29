#!/bin/bash
# Smoke build: restore + Debug build + tests of each trial 61 candidate, one at a time (untouched).
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
C=/home/user/cand61; L=/home/user/stylebro/.trials/trial61/logs/smoke; mkdir -p $L
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > $L/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' $L/$name.log) error lines"; }
run fs-build dotnet build $C/fluentscheduler-FluentScheduler/FluentScheduler.sln -p:NuGetAudit=false
run fs-test bash -c "cd $C/fluentscheduler-FluentScheduler && timeout 900 dotnet test FluentScheduler.sln --no-build"
run rs-build dotnet build $C/jbogard-Respawn/Respawn.sln -p:NuGetAudit=false
run rs-test bash -c "cd $C/jbogard-Respawn && timeout 600 dotnet test Respawn.UnitTests --no-build"
run em-build dotnet build $C/mganss-ExcelMapper/ExcelMapper.sln -p:NuGetAudit=false
run em-test bash -c "cd $C/mganss-ExcelMapper && timeout 900 dotnet test ExcelMapper.sln --no-build"
run ce-build dotnet build $C/jbevain-cecil/Mono.Cecil.sln -p:NuGetAudit=false
run ce-test bash -c "cd $C/jbevain-cecil && timeout 1200 dotnet test Mono.Cecil.sln --no-build -f net8.0"
echo SMOKE-DONE
