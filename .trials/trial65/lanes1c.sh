#!/bin/bash
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="base default"
./chain.sh FC >> FC/chain.out 2>&1
echo B-DONE > logs/laneB-done.txt
