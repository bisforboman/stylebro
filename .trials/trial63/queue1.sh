#!/bin/bash
# queue1.sh <keys...>: round 1 (0.8.0-trial63.1) on the given keys, one after another; resumable like queueRun.sh
cd /home/user/stylebro/.trials/trial63
one() {
  local k=$1 todo=""
  for t in ${RTAGS:-default dotnet}; do tail -1 $k/logs/$t-summary.tsv 2>/dev/null | grep -q DONE || todo="$todo $t"; done
  [ -n "$todo" ] && TAGS="$todo" NO_SOAK=1 ./chain.sh $k >> $k/chain.out 2>&1
  grep -q "${SOAK_LOG:-soak-compare} done" $k/chain.log || { ./soak.sh $k >> $k/chain.out 2>&1; echo "${SOAK_LOG:-soak-compare} done $(date -u +%H:%M)" >> $k/chain.log; }
}
for k in "$@"; do one $k; done
echo "QUEUE-DONE $*"
