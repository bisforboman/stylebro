#!/bin/bash
# prep.sh <key> <dest>: a fresh clone of the repository at the trial commit, with the local workarounds committed
set -e
KEY=$1; DEST=$2; T=/home/user/stylebro/.trials/trial61
. $T/$KEY/env.sh
rm -rf "$DEST"; git clone -q "$SRC" "$DEST"; cd "$DEST"; git checkout -q "$COMMIT"
# the real remote: SourceLink fails a project's MSBuild load on a local-path origin (trial 61, EM round 1)
git remote set-url origin "$(git -C "$SRC" remote get-url origin)"
git config user.name trial; git config user.email noreply@example.com
[ "${SUBMODULES:-}" = 1 ] && git submodule update --init --depth 1 -q
# global.json files that pin an SDK (not test resources named global.json)
for g in $(git ls-files | grep -i '\(^\|/\)global.json$'); do
  grep -q '"sdk"' "$g" && grep -q '"version"' "$g" && python3 -c "import json;d=json.load(open('$g'));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open('$g','w'),indent=2)"
done
# every nuget.config that clears the sources gets the trial feed (after its first <clear />)
for NC in $(git ls-files | grep -i '\(^\|/\)nuget.config$'); do
  python3 /home/user/stylebro/.trials/trial61/addfeed.py "$NC"
done
[ -n "${PREP:-}" ] && bash -c "$PREP"
git add -A; git commit -q -m "trial workarounds" || true
