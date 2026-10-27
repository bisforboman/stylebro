#!/bin/bash
cd /home/user/stylebro/.trials/trial56
TAGS="default dotnet" ./chain.sh EnumGen > EnumGen/chain.out 2>&1
./chain.sh FluentFTP > FluentFTP/chain.out 2>&1
