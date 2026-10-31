#!/bin/bash
# bro-sln.sh: bro with a folder argument replaced by <folder>/$FMT (Compare-Format.ps1 passes the clone; L4N has two solutions)
a=(); for x in "$@"; do if [ -n "${FMT:-}" ] && [ -d "$x" ] && [ -e "$x/$FMT" ]; then a+=("$x/$FMT"); else a+=("$x"); fi; done
exec bro "${a[@]}"
