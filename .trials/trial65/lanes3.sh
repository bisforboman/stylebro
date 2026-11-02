#!/bin/bash
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="default3" V=0.8.0-trial65.3 BRO_DIR=/home/user/bro65c
./chain.sh FC >> FC/chain.out 2>&1
echo C > logs/laneC3-done.txt
