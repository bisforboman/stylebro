#!/bin/bash
# Round 2 on 0.8.0-trial57.2 (the five fixes): default2 (host), dotnet2 (--engine dotnet-format), soak2; two chains
cd /home/user/stylebro/.trials/trial57
export BRO_DIR=/home/user/bro57b TAGS="default2 dotnet2" SOAK_LOG=soak2-compare SOAK_SRC=soak2-src SOAK_WORK=parity2
(./chain.sh DapperAOT > DapperAOT/chain2.out 2>&1) &
(for k in Bson ErrorProne OpenAPI; do ./chain.sh $k > $k/chain2.out 2>&1; done) &
wait
echo RUN2-DONE
