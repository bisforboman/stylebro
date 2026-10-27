#!/bin/bash
# base, default (host), --engine dotnet-format, then the soak (Compare-Format.ps1), two chains on 0.8.0-trial58.1
cd /home/user/stylebro/.trials/trial58
(for k in LightInject NPoco; do ./chain.sh $k > $k/chain.out 2>&1; done) &
(for k in CLU ETTS; do ./chain.sh $k > $k/chain.out 2>&1; done) &
wait
echo RUN-DONE
