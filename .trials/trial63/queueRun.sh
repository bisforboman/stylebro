#!/bin/bash
# default (host), --engine dotnet-format, then the soak, two lanes; resumable (a tag whose summary ends in DONE is skipped)
cd /home/user/stylebro/.trials/trial63
one() { # one <key>
  local k=$1 todo=""
  for t in ${RTAGS:-default dotnet}; do tail -1 $k/logs/$t-summary.tsv 2>/dev/null | grep -q DONE || todo="$todo $t"; done
  [ -n "$todo" ] && TAGS="$todo" NO_SOAK=1 ./chain.sh $k >> $k/chain.out 2>&1
  grep -q "${SOAK_LOG:-soak-compare} done" $k/chain.log || { ./soak.sh $k >> $k/chain.out 2>&1; echo "${SOAK_LOG:-soak-compare} done $(date -u +%H:%M)" >> $k/chain.log; }
}
(one NL; one PO) &
(one T4; one SF) &
wait
echo RUN-DONE
