#!/bin/bash
cd /home/user/stylebro/.trials/trial56
until [ -f logs/smoke.done ]; do sleep 15; done
./smoke.sh /home/user/cand/robinrodricks-FluentFTP FluentFTP.sln
./smoke.sh /home/user/cand/reactiveui-DynamicData src/DynamicData.sln
echo SMOKE2-DONE > logs/smoke2.done
