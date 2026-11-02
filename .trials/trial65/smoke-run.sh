#!/bin/bash
# the trial 65 candidates' smoke builds and tests (untouched)
C=/home/user/cand65; T=/home/user/stylebro/.trials/trial65
$T/smoke.sh "run SC-test dotnet test $C/serilog-serilog-sinks-console/serilog-sinks-console.sln" \
  "run AR-test dotnet test $C/stefanprodan-AspNetCoreRateLimit/AspNetCoreRateLimit.sln" \
  "run DE-test dotnet test $C/dynamicexpresso-DynamicExpresso/DynamicExpresso.sln" \
  "run FC-build dotnet build $C/ovska-FlameCsv/FlameCsv.sln"
