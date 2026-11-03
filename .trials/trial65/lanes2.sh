#!/bin/bash
# round 2 on 0.8.0-trial65.2 (host only)
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="default2" V=0.8.0-trial65.2 BRO_DIR=/home/user/bro65b
(for k in SC AR DE; do ./chain.sh $k >> $k/chain.out 2>&1; done; echo A > logs/laneA2-done.txt) &
(./chain.sh FC >> FC/chain.out 2>&1; echo B > logs/laneB2-done.txt) &
wait
