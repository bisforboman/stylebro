#!/bin/bash
# after the worker restart: L4N dotnet + soak, then MM default, dotnet + soak (one after the other: memory)
cd /home/user/stylebro/.trials/trial60
TAGS="dotnet" ./chain.sh L4N > L4N/chain3.out 2>&1
TAGS="default dotnet" ./chain.sh MM > MM/chain.out 2>&1
echo R-DONE
