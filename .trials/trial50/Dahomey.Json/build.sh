# usage: build.sh <clone> <tag>
. /home/user/stylebro/.trials/trial50/Dahomey.Json/env.sh
C=$1; T=$2; L=/home/user/stylebro/.trials/trial50/Dahomey.Json/logs
cd $C
for cfg in Debug Release; do
  s=$(date +%s); dotnet build src/Dahomey.Json.sln -c $cfg --no-incremental -p:TreatWarningsAsErrors=true -p:NuGetAudit=false > $L/$T-build-$cfg.log 2>&1; e=$?
  echo "$T build $cfg exit=$e $(( $(date +%s)-s ))s $(grep -E '^ *[0-9]+ (Warning|Error)\(s\)' $L/$T-build-$cfg.log | tr '\n' ' ')"
done
s=$(date +%s); dotnet test src/Dahomey.Json.sln -c Release --no-build -p:NuGetAudit=false > $L/$T-test.log 2>&1; e=$?
echo "$T test exit=$e $(( $(date +%s)-s ))s"; grep -E "^(Passed|Failed)!|Total tests|Passed:|Failed:" $L/$T-test.log | head
