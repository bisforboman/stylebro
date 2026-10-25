#!/bin/bash
# tests-repeat.sh: protobuf-net.Grpc's untouched tests twice more (Debug), to show which integration tests are flaky
export NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major CI=true EnableWindowsTargeting=true
L=/home/user/stylebro/.trials/trial55/Grpc/logs
until [ "$(grep -c rc= $L/repeat-preview.txt 2>/dev/null)" = 2 ]; do sleep 30; done
cd /home/user/w55/Grpc/base
for i in 1 2; do dotnet test protobuf-net.Grpc.slnx -c Debug > $L/base-test-repeat$i.log 2>&1; echo "repeat $i rc=$?" >> $L/base-test-repeat.txt; done
