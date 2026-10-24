#!/bin/bash
# soak.sh <repo-key>...: both engines on main cb47c2d + this branch (0.8.0-trial52.3), for the host soak after #298/#299
cd /home/user/stylebro/.trials/trial52
export PATH=/home/user/bro523:$PATH
for k in "$@"; do
  ./run.sh $k $k/s-default s-default && ./run.sh $k $k/s-host s-host --engine host
done
