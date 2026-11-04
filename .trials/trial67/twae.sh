#!/bin/bash
# usage: twae.sh <repo folder> <solution/project> <prefix> [dir]   (dir: where to build, default the repo itself)
# TreatWarningsAsErrors builds in Debug and Release, then the tests in Debug and Release (if no build error).
source /home/user/stylebro/.trials/trial67/env.sh
R=/home/user/stylebro/.trials/trial67/$1
D=${4:-$R}
cd $D || exit 1
mkdir -p $R/logs
P=$3
for c in Debug Release; do
  s=$(date +%s)
  dotnet build $2 -c $c -p:TreatWarningsAsErrors=true -p:NuGetAudit=false --no-incremental > $R/logs/$P-twae-$c.log 2>&1
  e=$?
  echo "twae build $c exit $e in $(( $(date +%s) - s ))s: $(grep -c ' error ' $R/logs/$P-twae-$c.log) error lines, $(grep -E ' error ' $R/logs/$P-twae-$c.log | sed 's/.*error \([A-Za-z0-9]*\).*/\1/' | sort -u | wc -l) distinct codes" | tee -a $R/logs/$P-summary.txt
done
