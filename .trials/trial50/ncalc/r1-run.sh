#!/bin/bash
# Re-check run: r1-run.sh <clone> <engine flag or empty>
cd /home/user/stylebro/.trials/trial50/ncalc/$1 || exit 1
. ../env.sh
E="$2"
L=../logs/$1
mkdir -p $L
run() { local name=$1; shift; local s=$(date +%s); "$@" > $L/$name.log 2>&1; local c=$?; echo "$name exit=$c $(( $(date +%s)-s ))s" | tee -a $L/summary.txt; }
commit() { git -c user.name=trial -c user.email=trial@users.noreply.github.com commit -qam "$1"; }
run preview bro preview init $E
run init bro init --write
cp ../r1-props/Directory.Build.props ../r1-props/Directory.Packages.props .
commit "trial: init + StyleBro.Analyzers"
run format bro format $E
commit "trial: format" || true
run format2 bro format $E
commit "trial: format 2" || true
run check bro check $E
s=$(date +%s); bro check $E --json > $L/check.json 2> $L/check-json.err; echo "check-json exit=$? $(( $(date +%s)-s ))s" | tee -a $L/summary.txt
run check-build bro check --build $E
s=$(date +%s); bro check --build $E --json > $L/check-build.json 2> $L/check-build-json.err; echo "check-build-json exit=$? $(( $(date +%s)-s ))s" | tee -a $L/summary.txt
run build-Debug dotnet build NCalc.slnx -c Debug
run build-Release dotnet build NCalc.slnx -c Release
run test-Debug dotnet test --solution NCalc.slnx
run test-Release dotnet test --solution NCalc.slnx -c Release
echo done | tee -a $L/summary.txt
