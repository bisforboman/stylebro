#!/bin/bash
# prep.sh <key> <dest>: a fresh clone of the repository at the trial commit, with the local workarounds committed
set -e
KEY=$1; DEST=$2; T=/home/user/stylebro/.trials/trial54
. $T/$KEY/env.sh
rm -rf "$DEST"; git clone -q "$SRC" "$DEST"; cd "$DEST"; git checkout -q "$COMMIT"
git config user.name trial; git config user.email noreply@example.com
[ "${SUBMODULES:-}" = 1 ] && { git config -f .gitmodules --get-regexp url | while read k u; do :; done; git submodule update --init -q; }
if [ -f global.json ] && grep -q '"version"' global.json; then
  python3 -c "import json;d=json.load(open('global.json'));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open('global.json','w'),indent=2)"
fi
NC=$(git ls-files | grep -i -m1 '^nuget.config$' || true)
if [ -n "$NC" ] && grep -q '<clear' "$NC"; then
  sed -i '0,/<clear \/>/s##<clear />\n    <add key="stylebro-trial" value="/home/user/feed54" />#' "$NC"
fi
git add -A; git commit -q -m "trial workarounds" || true
