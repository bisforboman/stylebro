#!/bin/bash
# spot-halfsetup.sh: EnumGenerators with the reference only in the root Directory.Build.props (build/ and the Benchmarks
# folder have their own props that don't import it): what init, format and check say about the project without StyleBro
T=/home/user/stylebro/.trials/trial56; L=$T/EnumGen/logs; D=/home/user/w56/EnumGen/half
export PATH=/home/user/bro56c:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
$T/prep.sh EnumGen $D > $L/half-prep.log 2>&1
cd $D && python3 $T/addref.py Directory.Build.props '<PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial56.3" PrivateAssets="all" />'
bro init --write > $L/half-init.log 2>&1; echo "init rc=$?" > $L/half-rc.txt
bro format > $L/half-format.log 2>&1; echo "format rc=$?" >> $L/half-rc.txt
bro check > $L/half-check.log 2>&1; echo "check rc=$?" >> $L/half-rc.txt
bro check --build > $L/half-checkbuild.log 2>&1; echo "checkbuild rc=$?" >> $L/half-rc.txt
