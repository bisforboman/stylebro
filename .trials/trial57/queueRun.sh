#!/bin/bash
# default (host), --engine dotnet-format, then the soak (Compare-Format.ps1), two chains on 0.8.0-trial57.1
cd /home/user/stylebro/.trials/trial57
(for k in Bson OpenAPI; do TAGS="default dotnet" ./chain.sh $k > $k/chain.out 2>&1; done) &
(for k in ErrorProne DapperAOT; do TAGS="default dotnet" ./chain.sh $k > $k/chain.out 2>&1; done) &
wait
echo RUN-DONE
