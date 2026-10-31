#!/bin/bash
# queue2.sh <keys...>: round 2 on 0.8.0-trial64.2 (BRO1519 leaves the gap above checked/unchecked to CSharpier), resumable
cd /home/user/stylebro/.trials/trial64
export V=0.8.0-trial64.2 BRO_DIR=/home/user/bro64-2 SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare RTAGS="default2 dotnet2"
exec ./queue1.sh "$@"
