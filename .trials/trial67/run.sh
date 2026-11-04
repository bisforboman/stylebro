#!/bin/bash
# usage: run.sh <repo folder> <solution/project> <round prefix> [init|migrate]
# Runs init/migrate (first round only), format, check, check --json, check --build and logs times + exit codes.
source /home/user/stylebro/.trials/trial67/env.sh
cd /home/user/stylebro/.trials/trial67/$1 || exit 1
mkdir -p logs
S=$2
P=$3
MODE=${4:-}
t() { local n=$1; shift; local s=$(date +%s); "$@" > logs/$P-$n.log 2>&1; local e=$?; echo "$n exit $e in $(( $(date +%s) - s ))s" | tee -a logs/$P-summary.txt; }
if [ -n "$MODE" ]; then t $MODE bro $MODE . --write --project $S; fi
t format bro format $S --timings
git status --short | grep -v '^??' | wc -l | sed 's/^/files changed: /' | tee -a logs/$P-summary.txt
git diff > logs/$P-tree.diff
t format2 bro format $S --timings
t check bro check $S
t checkjson bro check $S --json
t checkbuild bro check $S --build
echo done >> logs/$P-summary.txt
