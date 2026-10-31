#!/bin/bash
# round 2 lane B (SE, SL) once lane B's round 1 is done; lane A (AU, SA) once round 1's lane A is done
cd /home/user/stylebro/.trials/trial64
(until grep -q QUEUE-DONE logs/laneB2-r1.out; do sleep 20; done; ./queue2.sh SE SL > logs/laneB-r2.out 2>&1) &
(until [ -f logs/r1-done.txt ]; do sleep 20; done; ./queue2.sh AU SA > logs/laneA-r2.out 2>&1) &
wait; echo R2-DONE > logs/r2-done.txt
