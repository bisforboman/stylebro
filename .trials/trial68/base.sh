#!/bin/bash
# usage: base.sh <repo folder> <solution/project> [prefix]
source /home/user/stylebro/.trials/trial68/env.sh
cd /home/user/stylebro/.trials/trial68/$1 || exit 1
mkdir -p logs
p=${3:-base}
s=$(date +%s)
dotnet build $2 -c Debug -p:NuGetAudit=false > logs/$p-build-debug.log 2>&1
echo "build debug exit $? in $(( $(date +%s) - s ))s"
tail -4 logs/$p-build-debug.log
s=$(date +%s)
dotnet test ${SOLARG:-} $2 -c Debug --no-build -p:NuGetAudit=false --blame-hang-timeout 5m -p:EnableWindowsTargeting=true > logs/$p-test-debug.log 2>&1
echo "test debug exit $? in $(( $(date +%s) - s ))s"
grep -E "^(Passed|Failed)!|Total tests|Failed " logs/$p-test-debug.log | head -20
