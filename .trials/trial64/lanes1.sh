#!/bin/bash
# round 1: two lanes
cd /home/user/stylebro/.trials/trial64
(./queue1.sh AU SA > logs/laneA-r1.out 2>&1) &
(./queue1.sh SE SL > logs/laneB-r1.out 2>&1) &
wait; echo R1-DONE > logs/r1-done.txt
