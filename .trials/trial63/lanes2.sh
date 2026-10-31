#!/bin/bash
# round 2 after round 1: two lanes
cd /home/user/stylebro/.trials/trial63
true
(./queue2.sh NL PO > logs/laneA-r2.out 2>&1) &
(./queue2.sh T4 SF > logs/laneB-r2.out 2>&1) &
wait; echo R2-DONE > logs/r2-done.txt
