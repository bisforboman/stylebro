#!/bin/bash
# failed.sh <key> <a> <b>: the failing test names of two runs' test logs (Debug and Release), compared without durations.
# VSTest prints '  Failed Name [12 ms]', Microsoft.Testing.Platform 'failed Name (12ms)'; both are read, with totals.
L=/home/user/stylebro/.trials/trial63/$1/logs
f() { grep -h -E "^\s*(Failed|failed) [A-Za-z_]" $L/$1-test-*.log | sed -E 's/^\s*[Ff]ailed //; s/ (\[[^]]*\]|\([^)]*\))$//' | sort; }
t() { grep -h -E "^\s*(total|failed|succeeded|skipped):|^(Passed|Failed)! " $L/$1-test-*.log | sed -E 's/, Duration.*//' | tr -s ' ' | paste -sd' '; }
echo "$2: $(t $2)"; echo "$3: $(t $3)"
diff <(f $2) <(f $3) > /dev/null && echo "failing tests: same ($(f $2 | wc -l))" || { echo "failing tests: DIFFERENT"; diff <(f $2) <(f $3) | head; }
