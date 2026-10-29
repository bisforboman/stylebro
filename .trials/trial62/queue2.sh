#!/bin/bash
# queue2.sh <keys...>: round 2 on 0.8.0-trial62.2 (EditFile's encoding, init's snake_case field convention, sorted stopping errors), resumable
cd /home/user/stylebro/.trials/trial62
export V=0.8.0-trial62.2 BRO_DIR=/home/user/bro62b SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare RTAGS="default2 dotnet2"
exec ./queue1.sh "$@"
