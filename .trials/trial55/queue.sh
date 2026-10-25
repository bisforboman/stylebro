#!/bin/bash
# queue.sh <wait-key> <key>: start <key>'s chain when <wait-key>'s is done
cd /home/user/stylebro/.trials/trial55
until grep -q CHAIN-DONE $1/chain.log 2>/dev/null; do sleep 20; done
./chain.sh $2 > $2/chain.out 2>&1
