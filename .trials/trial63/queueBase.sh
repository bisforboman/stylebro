#!/bin/bash
# The untouched code's baselines (build, TWAE Debug/Release, tests), two lanes; skips a key whose base summary ends in DONE
cd /home/user/stylebro/.trials/trial63
one() { tail -1 $1/logs/base-summary.tsv 2>/dev/null | grep -q DONE || TAGS=base NO_SOAK=1 ./chain.sh $1 >> $1/chain.out 2>&1; }
(one NL; one PO) &
(one T4; one SF) &
wait
echo BASE-DONE
