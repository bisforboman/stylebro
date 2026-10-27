#!/bin/bash
# The untouched baselines (build, TWAE Debug/Release, tests) in two chains, before StyleBro is built
cd /home/user/stylebro/.trials/trial57
(for k in Bson OpenAPI; do TAGS=base NO_SOAK=1 ./chain.sh $k > $k/chain.out 2>&1; done) &
(for k in ErrorProne DapperAOT; do TAGS=base NO_SOAK=1 ./chain.sh $k > $k/chain.out 2>&1; done) &
wait
echo BASE-DONE
