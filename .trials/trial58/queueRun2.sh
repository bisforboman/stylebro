#!/bin/bash
# round 2 on 0.8.0-trial58.2 (the fixes): default (host), --engine dotnet-format, the soak, two chains
cd /home/user/stylebro/.trials/trial58
export TAGS="default2 dotnet2" SOAK_SRC=soak-src2 SOAK_WORK=parity2 SOAK_LOG=soak-compare2
(for k in LightInject NPoco; do ./chain.sh $k > $k/chain2.out 2>&1; done) &
(for k in ETTS CLU; do ./chain.sh $k > $k/chain2.out 2>&1; done) &
wait
echo RUN2-DONE
