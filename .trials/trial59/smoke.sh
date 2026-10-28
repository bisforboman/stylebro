#!/bin/bash
# Smoke build: restore + Debug build + tests of each trial 59 pick, one at a time (untouched apart from global.json).
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
C=/home/user/cand59; L=/home/user/stylebro/.trials/trial59/logs/smoke; mkdir -p $L
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > $L/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' $L/$name.log) error lines"; }
for g in $C/danielpalme-ReportGenerator/global.json $C/xoofx-SharpYaml/src/global.json $C/adamralph-bullseye/global.json $C/hazzik-DelegateDecompiler/global.json; do
  python3 -I -c "import json,sys;p=sys.argv[1];d=json.load(open(p));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open(p,'w'),indent=2)" $g
done
run bullseye-build dotnet build $C/adamralph-bullseye/Bullseye.slnx -p:NuGetAudit=false
run bullseye-test bash -c "cd $C/adamralph-bullseye && dotnet test --solution Bullseye.slnx --no-build"
run sharpyaml-build dotnet build $C/xoofx-SharpYaml/src/SharpYaml.slnx -p:NuGetAudit=false
run sharpyaml-test bash -c "cd $C/xoofx-SharpYaml/src && dotnet test SharpYaml.slnx --no-build"
run rg-build dotnet build $C/danielpalme-ReportGenerator/src/ReportGenerator.slnx -p:NuGetAudit=false
run rg-test bash -c "cd $C/danielpalme-ReportGenerator/src && dotnet test --solution ReportGenerator.slnx --no-build"
run dd-build dotnet build $C/hazzik-DelegateDecompiler/DelegateDecompiler.sln -p:NuGetAudit=false
run dd-test bash -c "cd $C/hazzik-DelegateDecompiler && dotnet test DelegateDecompiler.sln --no-build"
echo SMOKE-DONE
