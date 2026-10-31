#!/bin/bash
# queue1.sh <keys...>: base, then round 1 (default = host, dotnet = --engine dotnet-format), then the soak, one key after another; resumable
cd /home/user/stylebro/.trials/trial64
one() {
  local k=$1 todo=""
  for t in base ${RTAGS:-default dotnet}; do tail -1 $k/logs/$t-summary.tsv 2>/dev/null | grep -q DONE || todo="$todo $t"; done
  [ -n "$todo" ] && TAGS="$todo" NO_SOAK=1 ./chain.sh $k >> $k/chain.out 2>&1
  grep -q "${SOAK_LOG:-soak-compare} done" $k/chain.log || { ./soak.sh $k >> $k/chain.out 2>&1; echo "${SOAK_LOG:-soak-compare} done $(date -u +%H:%M)" >> $k/chain.log; }
}
for k in "$@"; do one $k; done
echo "QUEUE-DONE $*"
