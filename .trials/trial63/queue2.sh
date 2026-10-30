#!/bin/bash
# queue2.sh <keys...>: round 2 on 0.8.0-trial63.2 (BRO1126 keeps nullable '?', init's stylebro_allow_adjacent_single_line_members), resumable
cd /home/user/stylebro/.trials/trial63
export V=0.8.0-trial63.2 BRO_DIR=/home/user/bro63-2 SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare RTAGS="default2 dotnet2"
exec ./queue1.sh "$@"
