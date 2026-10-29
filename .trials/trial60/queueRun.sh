#!/bin/bash
# base, default (host), --engine dotnet-format, then the soak (Compare-Format.ps1), two chains on 0.8.0-trial60.1
cd /home/user/stylebro/.trials/trial60
(for k in ${CHAIN1:-RG Bullseye}; do ./chain.sh $k > $k/chain.out 2>&1; done) &
(for k in ${CHAIN2:-DD SharpYaml}; do ./chain.sh $k > $k/chain.out 2>&1; done) &
wait
echo RUN-DONE
