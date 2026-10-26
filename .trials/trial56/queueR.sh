#!/bin/bash
# queueR.sh: after the container restart (2026-10-26): what queueA3 left unfinished
cd /home/user/stylebro/.trials/trial56
export NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
rm -rf /home/user/w56/DynamicData/soak-parity
echo "restart after the container was reclaimed: soak again $(date -u +%H:%M)" >> DynamicData/chain.log
SOAKTAG=soak ./soak3.sh DynamicData
echo CHAIN-DONE >> DynamicData/chain.log
echo "round 2 (0.8.0-trial56.3: the BRO1117 and MSB4186 fixes)" >> FusionCache/chain.log
TAGS="default2 dotnet2" SOAKTAG=soak2 ./chain3.sh FusionCache > FusionCache/chain2.out 2>&1
cd /home/user/w56/DynamicData/dotnet && timeout 3600 dotnet test src/DynamicData.sln -c Release --blame-hang --blame-hang-timeout 10m > /home/user/stylebro/.trials/trial56/DynamicData/logs/dotnet-test-Release-rerun.log 2>&1
echo "dotnet test-Release rerun rc=$? $(date -u +%H:%M)" >> /home/user/stylebro/.trials/trial56/DynamicData/chain.log
echo QUEUE-R-DONE >> /home/user/stylebro/.trials/trial56/DynamicData/chain.log
