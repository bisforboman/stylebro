#!/bin/bash
# chain.sh <key>: base, default (no engine named: the host), dotnet (--engine dotnet-format) on fresh clones (W=/home/user/w63/<key>/<tag>)
K=$1; W=/home/user/w63/$K; T=/home/user/stylebro/.trials/trial63
export PATH=${BRO_DIR:-/home/user/bro63}:$PATH
for tag in ${TAGS:-base default dotnet}; do
  $T/prep.sh $K $W/$tag > $T/$K/prep-$tag.log 2>&1 || { echo "prep $tag failed" >> $T/$K/chain.log; exit 1; }
  echo "$tag start $(date -u +%H:%M)" >> $T/$K/chain.log
  if [ "${tag#dotnet}" != "$tag" ]; then $T/run.sh $K $W/$tag $tag --engine dotnet-format; else $T/run.sh $K $W/$tag $tag; fi
  echo "$tag done $(date -u +%H:%M)" >> $T/$K/chain.log
done
[ -z "${NO_SOAK:-}" ] && $T/soak.sh $K
echo CHAIN-DONE >> $T/$K/chain.log
