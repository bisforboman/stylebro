#!/bin/bash
# queue2.sh <keys...>: round 2 on 0.8.0-trial62.4 (tab indentation, BRO1120 frames, the preview patch's --binary, EditFile's encoding, init's snake_case field convention for BRO1308, sorted stopping errors), resumable
cd /home/user/stylebro/.trials/trial62
export V=0.8.0-trial62.4 BRO_DIR=/home/user/bro62d SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare RTAGS="default2 dotnet2"
exec ./queue1.sh "$@"
