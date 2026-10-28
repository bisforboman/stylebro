#!/bin/bash
# soak.sh <key>: the engine soak's Compare-Format.ps1 on a clone with step 1 (the package reference, init --write) committed
K=$1; T=/home/user/stylebro/.trials/trial59; W=/home/user/w59/$K
export PATH=${BRO_DIR:-/home/user/bro59}:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
. $T/$K/env.sh
$T/prep.sh $K $W/${SOAK_SRC:-soak-src} > $T/$K/prep-soak.log 2>&1 || { echo "soak prep failed" >> $T/$K/chain.log; exit 1; }
cd $W/${SOAK_SRC:-soak-src} && bash -c "$SETUP" >> $T/$K/prep-soak.log 2>&1 && git add -A && git commit -q -m "stylebro step 1" 
echo "soak start $(date -u +%H:%M)" >> $T/$K/chain.log
pwsh $T/../../scripts/format-speed/Compare-Format.ps1 -Repository $W/${SOAK_SRC:-soak-src} -Work $W/${SOAK_WORK:-parity} -Bro bro > $T/$K/logs/${SOAK_LOG:-soak-compare}.log 2>&1
echo "soak done rc=$? $(date -u +%H:%M)" >> $T/$K/chain.log
