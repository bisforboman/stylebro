#!/bin/bash
# compare.sh <key> [a] [b]: default vs host: exit codes, trees (diff patch), --json, the logs with paths normalized
K=$1; A=${2:-default}; B=${3:-dotnet}; L=/home/user/stylebro/.trials/trial64/$K/logs
diff <(cut -f1,2 $L/$A-summary.tsv) <(cut -f1,2 $L/$B-summary.tsv) && echo "exit codes: same"
cmp -s $L/$A-diff.patch $L/$B-diff.patch && echo "tree: same" || { echo "tree: DIFFERENT"; diff <(grep '^diff' $L/$A-diff.patch) <(grep '^diff' $L/$B-diff.patch) | head; }
for f in check.json checkbuild.json; do diff -q <(sed "s#/$A\b#/X#g" $L/$A-$f) <(sed "s#/$B\b#/X#g" $L/$B-$f) >/dev/null && echo "$f: same" || echo "$f: DIFFERENT"; done
S=/tmp/trial64-cmp; mkdir -p $S
T=/home/user/stylebro/.trials/trial64
for s in preview setup format check check-full checkbuild; do
  diff <(python3 $T/norm.py $L/$A-$s.log $K) <(python3 $T/norm.py $L/$B-$s.log $K) > $S/cmp-$K-$s.txt && echo "$s log: same" || { echo "$s log: differs ($(grep -c '^[<>]' $S/cmp-$K-$s.txt) lines)"; head -8 $S/cmp-$K-$s.txt; }
done
