#!/bin/bash
# Smoke build: restore + Debug build + tests of each trial 60 pick, one at a time (untouched).
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
C=/home/user/cand60; L=/home/user/stylebro/.trials/trial60/logs/smoke; mkdir -p $L
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > $L/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' $L/$name.log) error lines"; }
run mw-build dotnet build $C/khellang-Middleware/Hellang.Middleware.sln -p:NuGetAudit=false
run mw-test bash -c "cd $C/khellang-Middleware && dotnet test Hellang.Middleware.sln --no-build"
run mm-build dotnet build $C/Cysharp-MasterMemory/MasterMemory.sln -p:NuGetAudit=false
run mm-test bash -c "cd $C/Cysharp-MasterMemory && dotnet test MasterMemory.sln --no-build"
run l4n-build dotnet build $C/apache-logging-log4net/src/log4net.sln -p:NuGetAudit=false
run l4n-test bash -c "cd $C/apache-logging-log4net/src && dotnet test log4net.sln --no-build -f net10.0"
run ec-build dotnet build $C/dotnetcore-EasyCaching/EasyCaching.sln -p:NuGetAudit=false
echo SMOKE-DONE
