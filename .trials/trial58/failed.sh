#!/bin/bash
# failed.sh <key> <a> <b>: the failing test names of two runs' test logs (Debug and Release), compared without durations
L=/home/user/stylebro/.trials/trial58/$1/logs
f() { grep -h -E "^\s+Failed " $L/$1-test-*.log | sed -E 's/ \[[^]]*\]$//' | sort; }
diff <(f $2) <(f $3) > /dev/null && echo "failing tests: same ($(f $2 | wc -l))" || { echo "failing tests: DIFFERENT"; diff <(f $2) <(f $3) | head; }
