#!/bin/bash
# recheck.sh <repo-key>...: the final re-check, default then host, one repository after another
cd /home/user/stylebro/.trials/trial51
for k in "$@"; do
  (cd $k && ../run.sh $k r-default r-default && ../run.sh $k r-host r-host --engine host)
done
