#!/bin/bash
# prints record path for repo folder name
p=$(readlink -f /home/user/stylebro/.trials/trial67/$1 | tr a-z A-Z)
h=$(printf %s "$p" | sha256sum | cut -c1-16 | tr a-f A-F)
echo /tmp/stylebro-kept/$h.tsv
