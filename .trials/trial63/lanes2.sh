#!/bin/bash
# round 2 after round 1: two lanes
cd /home/user/stylebro/.trials/trial63
until grep -q RUN-DONE logs/laneR1.out; do sleep 20; done
(./queue2.sh NL PO > logs/laneA-r2.out 2>&1) &
(./queue2.sh T4 SF > logs/laneB-r2.out 2>&1) &
wait; echo R2-DONE > logs/r2-done.txt
