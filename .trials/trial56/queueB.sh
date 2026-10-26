#!/bin/bash
cd /home/user/stylebro/.trials/trial56
until [ -f logs/smoke2.done ]; do sleep 20; done
./chain.sh EnumGen > EnumGen/chain.out 2>&1
./chain.sh FluentFTP > FluentFTP/chain.out 2>&1
