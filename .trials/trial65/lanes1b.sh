#!/bin/bash
# round 1 rerun after adding the trial feed to the user NuGet.Config: default only for SC AR DE
cd /home/user/stylebro/.trials/trial65
export NO_SOAK=1 TAGS="default"
for k in SC AR DE; do ./chain.sh $k >> $k/chain.out 2>&1; done
echo A-DONE > logs/laneA-done.txt
