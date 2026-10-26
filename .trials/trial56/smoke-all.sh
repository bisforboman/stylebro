#!/bin/bash
cd /home/user/stylebro/.trials/trial56
./smoke.sh /home/user/cand/NickCraver-StackExchange.Exceptional StackExchange.Exceptional.sln
./smoke.sh /home/user/cand/ZiggyCreatures-FusionCache ZiggyCreatures.FusionCache.slnx
./smoke.sh /home/user/cand/andrewlock-NetEscapades.EnumGenerators NetEscapades.EnumGenerators.sln
echo SMOKE-DONE > logs/smoke.done
