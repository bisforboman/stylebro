#!/bin/bash
# the trial 64 candidates' smoke builds and tests (untouched apart from global.json SDK pins)
C=/home/user/cand64; T=/home/user/stylebro/.trials/trial64
fixg() { for g in $(git -C $1 ls-files | grep -i '\(^\|/\)global.json$'); do grep -q '"sdk"' $1/$g && grep -q '"version"' $1/$g && python3 -c "import json;p='$1/$g';d=json.load(open(p));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open(p,'w'),indent=2)"; done; }
for d in serilog-serilog-expressions serilog-serilog-extensions-logging TNG-ArchUnitNET; do fixg $C/$d; done
$T/smoke.sh "run SA-build dotnet build $C/serilog-serilog-sinks-async/serilog-sinks-async.sln" "run SA-test dotnet test $C/serilog-serilog-sinks-async/serilog-sinks-async.sln" \
  "run SL-build dotnet build $C/serilog-serilog-extensions-logging/serilog-extensions-logging.sln" "run SL-test dotnet test $C/serilog-serilog-extensions-logging/serilog-extensions-logging.sln" \
  "run SE-build dotnet build $C/serilog-serilog-expressions/serilog-expressions.sln" "run SE-test dotnet test $C/serilog-serilog-expressions/serilog-expressions.sln" \
  "run AU-build dotnet build $C/TNG-ArchUnitNET/ArchUnit.sln" "run AU-test dotnet test $C/TNG-ArchUnitNET/ArchUnit.sln"
