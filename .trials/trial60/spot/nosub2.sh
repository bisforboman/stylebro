#!/bin/bash
# MW without its submodule checked out (git clone without --recursive): setup, format, check, check --build
T=/home/user/stylebro/.trials/trial60; W=/home/user/w60/MW/nosub2; export V=0.8.0-trial60.2; L=$T/MW/logs
export PATH=/home/user/bro60b:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true
. $T/MW/env.sh; SUBMODULES=0
rm -rf $W; git clone -q $SRC $W; cd $W; git checkout -q $COMMIT; git config user.name trial; git config user.email noreply@example.com
python3 $T/addfeed.py NuGet.config; bash -c "$PREP"; git add -A; git commit -qm "trial workarounds"
for s in "base-build dotnet build $SLN" "setup bash -c \"\$SETUP\"" "format bro format" "check bro check" "checkbuild bro check --build"; do
  n=${s%% *}; c=${s#* }; t0=$(date +%s); eval "$c" > $L/nosub2-$n.log 2>&1; echo -e "$n\t$?\t$(( $(date +%s) - t0 ))s" >> $L/nosub2-summary.tsv
done
git status --short > $L/nosub2-status.txt; git diff > $L/nosub2-diff.patch
echo DONE >> $L/nosub2-summary.tsv
