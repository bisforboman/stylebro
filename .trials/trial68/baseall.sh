#!/bin/bash
# Baseline: Debug build + tests of the untouched code, one repository at a time.
cd /home/user/stylebro/.trials/trial68
export DOTNET_ROLL_FORWARD=Major
for spec in "CacheManager CacheManager.sln" "BedrockFramework BedrockFramework.sln" "sendgrid-csharp SendGrid.sln" "avro-full lang/csharp/Avro.sln"; do
  set -- $spec
  echo "=== $1"
  timeout 2400 bash base.sh $1 $2
done
echo ALLDONE
