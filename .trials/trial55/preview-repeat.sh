#!/bin/bash
# preview-repeat.sh: protobuf-net.Grpc's preview once more on each engine (fresh clones), to see whether the summary's
# "reported changes" count is stable per engine
T=/home/user/stylebro/.trials/trial55; W=/home/user/w55/Grpc
export PATH=/home/user/bro55:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major CI=true EnableWindowsTargeting=true
until grep -q "d rc" /home/user/w55/ZLinq/dbg/done 2>/dev/null; do sleep 30; done
for e in host dotnet; do
  $T/prep.sh Grpc $W/prev-$e > /dev/null 2>&1
  cd $W/prev-$e; if [ $e = dotnet ]; then bro preview init --engine dotnet-format; else bro preview init; fi > $T/Grpc/logs/repeat-preview-$e.log 2>&1
  echo "$e rc=$? $(grep '^Format: ' $T/Grpc/logs/repeat-preview-$e.log)" >> $T/Grpc/logs/repeat-preview.txt
done
