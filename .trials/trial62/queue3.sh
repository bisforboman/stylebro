#!/bin/bash
# queue3.sh <keys...>: round 3 on 0.8.0-trial62.5 (round 2's fixes + BRO1505/BRO1506 order, the support-warning note's run), resumable
cd /home/user/stylebro/.trials/trial62
export V=0.8.0-trial62.5 BRO_DIR=/home/user/bro62e SOAK_SRC=soak3-src SOAK_WORK=parity3 SOAK_LOG=soak3-compare RTAGS="default3 dotnet3"
exec ./queue1.sh "$@"
