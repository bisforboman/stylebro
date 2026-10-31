#!/bin/bash
# round 2: two lanes
cd /home/user/stylebro/.trials/trial64
(./queue2.sh AU SA > logs/laneA-r2.out 2>&1) &
(./queue2.sh SE SL > logs/laneB-r2.out 2>&1) &
wait; echo R2-DONE > logs/r2-done.txt
