#!/bin/bash
# the trial 63 candidates' smoke builds and tests (untouched apart from global.json SDK pins)
C=/home/user/cand63
fixg() { for g in $(git -C $1 ls-files | grep -i '\(^\|/\)global.json$'); do grep -q '"sdk"' $1/$g && python3 -c "import json;p='$1/$g';d=json.load(open(p));d['sdk']['version']='10.0.100';d['sdk']['rollForward']='latestFeature';json.dump(d,open(p,'w'),indent=2)"; done; }
fixg $C/serilog-serilog-sinks-file
./smoke.sh "run SF-build dotnet build $C/serilog-serilog-sinks-file/serilog-sinks-file.sln" "run SF-test dotnet test $C/serilog-serilog-sinks-file/serilog-sinks-file.sln" \
  "run PO-build dotnet build $C/tonerdo-pose/Pose.sln" "run PO-test dotnet test $C/tonerdo-pose/Pose.sln" \
  "run NL-build dotnet build $C/nlog-NLog.Extensions.Logging/NLog.Extensions.Logging.sln" "run NL-test dotnet test $C/nlog-NLog.Extensions.Logging/NLog.Extensions.Logging.sln -f net8.0" \
  "run T4-build dotnet build $C/mono-t4/Mono.TextTemplating.sln" "run T4-test dotnet test $C/mono-t4/Mono.TextTemplating.sln"
