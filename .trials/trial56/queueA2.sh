#!/bin/bash
cd /home/user/stylebro/.trials/trial56
until grep -q CHAIN-DONE FusionCache/chain.log 2>/dev/null; do sleep 30; done
./chain2.sh DynamicData > DynamicData/chain.out 2>&1
echo "round 2 (0.8.0-trial56.2, the BRO1117 fix)" >> FusionCache/chain.log
TAGS="default2 dotnet2" SOAKTAG=soak2 ./chain2.sh FusionCache > FusionCache/chain2.out 2>&1
