#!/bin/bash
# timings.sh <round suffix, e.g. 3>: format / check --build / full check seconds per engine, and the soak's times
cd /home/user/stylebro/.trials/trial63
for k in SF PO NL T4; do
  t() { awk -F'\t' -v s="$2" '$1==s {print $3}' $k/logs/$1-summary.tsv 2>/dev/null | tr -d s; }
  printf '%s: format %s/%s, check --build %s/%s, full check %s/%s, check (record) %s/%s' $k "$(t default$1 format)" "$(t dotnet$1 format)" "$(t default$1 checkbuild)" "$(t dotnet$1 checkbuild)" "$(t default$1 check-full)" "$(t dotnet$1 check-full)" "$(t default$1 check)" "$(t dotnet$1 check)"
  L=$k/logs/soak$1-compare.log; [ "$1" = "" ] && L=$k/logs/soak-compare.log
  printf '; soak check %s/%s, format %s/%s\n' "$(grep 'host check ' $L | awk '{print $3}')" "$(grep 'dotnet-format check ' $L | awk '{print $3}')" "$(grep 'host format ' $L | awk '{print $3}')" "$(grep 'dotnet-format format ' $L | awk '{print $3}')"
done
