#!/bin/bash
# Round 2 on 0.8.0-trial59.2 (the fixes): host (default2), --engine dotnet-format (dotnet2), then the soak, two chains.
cd /home/user/stylebro/.trials/trial59
export TAGS="default2 dotnet2" SOAK_SRC=soak-src2 SOAK_WORK=parity2 SOAK_LOG=soak-compare2
(for k in SharpYaml RG; do ./chain.sh $k > $k/chain2.out 2>&1; done) &
(for k in DD Bullseye; do ./chain.sh $k > $k/chain2.out 2>&1; done) &
wait
echo RUN2-DONE
