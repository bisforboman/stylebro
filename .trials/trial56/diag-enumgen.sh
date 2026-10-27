#!/bin/bash
export PATH=/home/user/bro56:$PATH NuGetAudit=false NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
L=/home/user/stylebro/.trials/trial56/EnumGen/logs
cd /home/user/w56/EnumGen/parity/soak-src-host && bro check --verbosity diagnostic > $L/diag-host-check.log 2>&1; echo "host rc=$?" >> $L/diag.txt
cd /home/user/w56/EnumGen/parity/soak-src-dotnet-format && bro check --engine dotnet-format --verbosity diagnostic > $L/diag-dotnet-check.log 2>&1; echo "dotnet rc=$?" >> $L/diag.txt
