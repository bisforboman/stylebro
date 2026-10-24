#!/bin/bash
# fixrun.sh <key> <tag> [engine args]: the trial steps with the fixed build (0.8.0-trial53.2) on a fresh clone
K=$1; T=$2; shift 2; export PATH=/home/user/bro532:$PATH
cd /home/user/stylebro/.trials/trial53 && ./prep.sh $K /home/user/w53/$K/$T > $K/prep-$T.log 2>&1 && ./run.sh $K /home/user/w53/$K/$T $T "$@"; echo "$T done $(date -u +%H:%M)" >> $K/chain.log
