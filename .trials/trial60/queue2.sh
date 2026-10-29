#!/bin/bash
# Round 2 on 0.8.0-trial60.2, resumable after a worker restart: a tag whose summary ends in DONE is skipped, so is a soak
# that finished. Two lanes (L4N, MW | EC, MM); the untouched code's base logs are round 1's.
cd /home/user/stylebro/.trials/trial60
export V=0.8.0-trial60.2 BRO_DIR=/home/user/bro60b SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare
one() { # one <key>
  local k=$1 todo=""
  for t in default2 dotnet2; do tail -1 $k/logs/$t-summary.tsv 2>/dev/null | grep -q DONE || todo="$todo $t"; done
  [ -n "$todo" ] && TAGS="$todo" NO_SOAK=1 ./chain.sh $k >> $k/chain2.out 2>&1
  grep -q "soak2 done" $k/chain.log || { ./soak.sh $k >> $k/chain2.out 2>&1; echo "soak2 done $(date -u +%H:%M)" >> $k/chain.log; }
}
(one L4N; one MW) &
(one EC; one MM) &
wait
echo ROUND2-DONE
