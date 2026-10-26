#!/bin/bash
# selfcheck.sh: the checks before pushing (CLAUDE.md): build, tests, the CI self-check, the changelog check
cd /home/user/stylebro; L=/home/user/stylebro/.trials/trial56/logs
export NUGET_CERT_REVOCATION_MODE=offline
dotnet build StyleBro.slnx > $L/sc-build.log 2>&1; echo "build rc=$?" > $L/sc-rc.txt
dotnet test StyleBro.slnx --no-build > $L/sc-test.log 2>&1; echo "test rc=$?" >> $L/sc-rc.txt
dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples > $L/sc-format.log 2>&1; echo "format rc=$?" >> $L/sc-rc.txt
python3 scripts/changelog.py --check > $L/sc-changelog.log 2>&1; echo "changelog rc=$?" >> $L/sc-rc.txt
echo SC-DONE >> $L/sc-rc.txt
