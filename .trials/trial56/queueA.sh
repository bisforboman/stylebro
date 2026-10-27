#!/bin/bash
cd /home/user/stylebro/.trials/trial56
until grep -q CHAIN-DONE FusionCache/chain.log 2>/dev/null; do sleep 30; done
./chain.sh DynamicData > DynamicData/chain.out 2>&1
