#!/bin/bash
# Round 3 on 0.8.0-trial61.3 (BRO1107/BRO1108 indent from the name line when BRO1109 moves the paren; Compare-Format keeps origin)
cd /home/user/stylebro/.trials/trial61
export V=0.8.0-trial61.3 BRO_DIR=/home/user/bro61c SOAK_SRC=soak3-src SOAK_WORK=parity3 SOAK_LOG=soak3-compare RTAGS="default3 dotnet3"
exec ./queueRun.sh
