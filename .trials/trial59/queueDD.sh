#!/bin/bash
# DD again with the reference in src/Directory.Build.props (round 1's setup put it in a root props src/'s shadows): the
# base logs stand, so default (host), --engine dotnet-format and the soak.
cd /home/user/stylebro/.trials/trial59
mv DD/chain.log DD/chain-wrongsetup.log 2>/dev/null
TAGS="default dotnet" ./chain.sh DD > DD/chain.out 2>&1
echo DD-DONE
