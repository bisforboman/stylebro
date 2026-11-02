#!/bin/bash
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="default"
./chain.sh DE >> DE/chain.out 2>&1
echo D-DONE > logs/laneD-done.txt
