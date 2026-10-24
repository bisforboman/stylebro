#!/bin/bash
# chain.sh <key>: base, default, host on fresh clones (W=/home/user/w54/<key>/<tag>)
K=$1; W=/home/user/w54/$K; T=/home/user/stylebro/.trials/trial54
for tag in base default host; do
  $T/prep.sh $K $W/$tag > $T/$K/prep-$tag.log 2>&1 || { echo "prep $tag failed" >> $T/$K/chain.log; exit 1; }
  if [ $tag = host ]; then $T/run.sh $K $W/$tag $tag --engine host; else $T/run.sh $K $W/$tag $tag; fi
  echo "$tag done $(date -u +%H:%M)" >> $T/$K/chain.log
done
echo CHAIN-DONE >> $T/$K/chain.log
