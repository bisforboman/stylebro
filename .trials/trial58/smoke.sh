#!/bin/bash
# Smoke build: restore + Debug build + tests of each random pick (trial 58), one at a time.
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
C=/home/user/cand58; L=/home/user/stylebro/.trials/trial58/logs/smoke; mkdir -p $L
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > $L/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' $L/$name.log) error lines"; }
python3 -I -c "import json;p='$C/seesharper-LightInject/global.json';d=json.load(open(p));d['sdk']['version']='10.0.100';json.dump(d,open(p,'w'),indent=2)"
run lightinject-build dotnet build $C/seesharper-LightInject/LightInject.sln -p:NuGetAudit=false
run lightinject-test bash -c "cd $C/seesharper-LightInject && dotnet test LightInject.sln --no-build"
run clu-build dotnet build $C/natemcmaster-CommandLineUtils/CommandLineUtils.sln -p:NuGetAudit=false
run clu-test bash -c "cd $C/natemcmaster-CommandLineUtils && dotnet test CommandLineUtils.sln --no-build"
run npoco-build dotnet build $C/schotime-NPoco/NPoco.sln -p:NuGetAudit=false
run npoco-test bash -c "cd $C/schotime-NPoco && dotnet test NPoco.sln --no-build"
echo done
