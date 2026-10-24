#!/bin/bash
# recheck.sh <repo-key>...: the final re-check, default then host, one repository after another
cd /home/user/stylebro/.trials/trial52
for k in "$@"; do
  ./run.sh $k $k/r-default r-default && ./run.sh $k $k/r-host r-host --engine host
done
