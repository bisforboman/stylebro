#!/bin/bash
cd /home/user/stylebro/.trials/trial55; until grep -q CHAIN-DONE FeatureManagement/chain.log 2>/dev/null; do sleep 30; done; ./soak.sh ZLinq
