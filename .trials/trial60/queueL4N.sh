#!/bin/bash
# L4N again after chain 1 (MW) finishes, with FMT set (two solutions)
cd /home/user/stylebro/.trials/trial60
while pgrep -f "chain.s[h] MW" > /dev/null; do sleep 20; done
TAGS="default dotnet" ./chain.sh L4N > L4N/chain2.out 2>&1
echo L4N-DONE
