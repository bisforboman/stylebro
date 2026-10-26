#!/bin/bash
cd /home/user/stylebro/.trials/trial56
TAGS="default dotnet" ./chain3.sh DynamicData > DynamicData/chain.out 2>&1
echo "round 2 (0.8.0-trial56.3: the BRO1117 and MSB4186 fixes)" >> FusionCache/chain.log
TAGS="default2 dotnet2" SOAKTAG=soak2 ./chain3.sh FusionCache > FusionCache/chain2.out 2>&1
