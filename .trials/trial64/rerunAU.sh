#!/bin/bash
# AU round 2, host run again on a fresh clone: the first one's tree was changed by a 'bro format' I ran in that clone while
# its TWAE build restored it (34 <inheritdoc/> lines in ArchUnitNETTests); its logs are in AU/logs/contaminated/
cd /home/user/stylebro/.trials/trial64
until [ -f logs/r2-done.txt ]; do sleep 20; done
mv AU/logs/default2-* AU/logs/contaminated/
export V=0.8.0-trial64.2 BRO_DIR=/home/user/bro64-2
TAGS=default2 NO_SOAK=1 ./chain.sh AU >> AU/chain.out 2>&1
echo RERUN-DONE > logs/rerunAU-done.txt
