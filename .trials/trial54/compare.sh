#!/bin/bash
# compare.sh <key> [a] [b]: default vs host: exit codes, trees (diff patch), --json, the logs with paths normalized
K=$1; A=${2:-default}; B=${3:-host}; L=/home/user/stylebro/.trials/trial54/$K/logs
diff <(cut -f1,2 $L/$A-summary.tsv) <(cut -f1,2 $L/$B-summary.tsv) && echo "exit codes: same"
cmp -s $L/$A-diff.patch $L/$B-diff.patch && echo "tree: same" || { echo "tree: DIFFERENT"; diff <(grep '^diff' $L/$A-diff.patch) <(grep '^diff' $L/$B-diff.patch) | head; }
for f in check.json checkbuild.json; do diff -q <(sed "s#/$A\b#/X#g" $L/$A-$f) <(sed "s#/$B\b#/X#g" $L/$B-$f) >/dev/null && echo "$f: same" || echo "$f: DIFFERENT"; done
for s in preview setup format check checkbuild; do
  n() { sed -E "s#/home/user/w54/$K/[a-z]+#R#g; s#stylebro-preview-[A-Za-z0-9]+#P#g; s#[0-9]+(\.[0-9]+)?s\b#Ns#g; s#[0-9]+m [0-9]+s#NmNs#g" "$1"; }
  diff <(n $L/$A-$s.log) <(n $L/$B-$s.log) > /tmp/claude-0/-home-user-stylebro/77d5a9f4-7ba2-5be8-8598-70c48b343380/scratchpad/cmp-$K-$s.txt && echo "$s log: same" || { echo "$s log: differs ($(grep -c '^[<>]' /tmp/claude-0/-home-user-stylebro/77d5a9f4-7ba2-5be8-8598-70c48b343380/scratchpad/cmp-$K-$s.txt) lines)"; head -8 /tmp/claude-0/-home-user-stylebro/77d5a9f4-7ba2-5be8-8598-70c48b343380/scratchpad/cmp-$K-$s.txt; }
done
