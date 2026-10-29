#!/bin/bash
# Round 2 on 0.8.0-trial61.2 (the BRO1503 convention; prep restores the real origin), resumable like queueRun.sh
cd /home/user/stylebro/.trials/trial61
export V=0.8.0-trial61.2 BRO_DIR=/home/user/bro61b SOAK_SRC=soak2-src SOAK_WORK=parity2 SOAK_LOG=soak2-compare RTAGS="default2 dotnet2"
exec ./queueRun.sh
