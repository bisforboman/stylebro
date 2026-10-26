#!/bin/bash
# chain2.sh <key>: like chain.sh with the 0.8.0-trial56.2 tool (/home/user/bro56b); tags from TAGS (a tag starting with
# 'dotnet' names --engine dotnet-format), default "base default dotnet"; soak2.sh after, unless NO_SOAK
K=$1; W=/home/user/w56/$K; T=/home/user/stylebro/.trials/trial56
export PATH=/home/user/bro56b:$PATH
for tag in ${TAGS:-base default dotnet}; do
  $T/prep.sh $K $W/$tag > $T/$K/prep-$tag.log 2>&1 || { echo "prep $tag failed" >> $T/$K/chain.log; exit 1; }
  echo "$tag start $(date -u +%H:%M)" >> $T/$K/chain.log
  case $tag in dotnet*) $T/run.sh $K $W/$tag $tag --engine dotnet-format;; *) $T/run.sh $K $W/$tag $tag;; esac
  echo "$tag done $(date -u +%H:%M)" >> $T/$K/chain.log
done
[ -z "${NO_SOAK:-}" ] && SOAKTAG=${SOAKTAG:-soak} $T/soak2.sh $K
echo CHAIN-DONE >> $T/$K/chain.log
