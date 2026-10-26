#!/bin/bash
# soak.sh <key>: the engine soak's Compare-Format.ps1 on a clone with step 1 (the package reference, init --write) committed
K=$1; T=/home/user/stylebro/.trials/trial56; W=/home/user/w56/$K
export PATH=/home/user/bro56b:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
. $T/$K/env.sh
$T/prep.sh $K $W/$SOAKTAG-src > $T/$K/prep-$SOAKTAG.log 2>&1 || { echo "$SOAKTAG prep failed" >> $T/$K/chain.log; exit 1; }
cd $W/$SOAKTAG-src && bash -c "$SETUP" >> $T/$K/prep-$SOAKTAG.log 2>&1 && git add -A && git commit -q -m "stylebro step 1" 
echo "$SOAKTAG start $(date -u +%H:%M)" >> $T/$K/chain.log
pwsh $T/../../scripts/format-speed/Compare-Format.ps1 -Repository $W/$SOAKTAG-src -Work $W/$SOAKTAG-parity -Bro bro > $T/$K/logs/$SOAKTAG-compare.log 2>&1
echo "$SOAKTAG done rc=$? $(date -u +%H:%M)" >> $T/$K/chain.log
