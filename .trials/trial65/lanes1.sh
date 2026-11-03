#!/bin/bash
# round 1, host engine only (owner's decision 2026-11-02: no soak): base, then default
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="base default"
(for k in SC AR DE; do ./chain.sh $k >> $k/chain.out 2>&1; done > logs/laneA-r1.out 2>&1) &
(./chain.sh FC >> FC/chain.out 2>&1) &
wait; echo R1-DONE > logs/r1-done.txt
