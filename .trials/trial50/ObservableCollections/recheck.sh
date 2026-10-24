#!/bin/bash
# Re-check: one engine ($1 = default|host) on a fresh clone of base at the trial commit.
set -u
R=/home/user/stylebro/.trials/trial50/ObservableCollections
. $R/env.sh
e=$1; E=""; [ "$e" = host ] && E="--engine host"
export V=${V:-0.8.0-trial50.2}; L=$R/logs/${LOGDIR:-recheck}; mkdir -p $L
d=$R/${PFX:-r}-$e; rm -rf $d; git clone -q $R/base $d; cd $d; git checkout -q 8ed7ad87b35885b2540158d9188d710b1cf32763
run() { local n=$1; shift; local s=$(date +%s.%N); "$@" > $L/$e-$n.log 2>&1; local x=$?; echo "exit $x time $(echo "$(date +%s.%N) - $s" | bc)" >> $L/$e-$n.log; echo "$e $n: exit $x"; }
gc() { git add -A; git -c user.email=t@t -c user.name=t commit -qm "$1"; }
run 1-preview-init bro preview init
run 2-init bro init --write
python3 - <<'EOF'
p='Directory.Build.props';s=open(p,encoding='utf-8').read()
s=s.replace('</Project>','  <ItemGroup>\n    <PackageReference Include="StyleBro.Analyzers" Version="'+__import__('os').environ['V']+'" PrivateAssets="all" />\n  </ItemGroup>\n</Project>')
open(p,'w',encoding='utf-8').write(s)
EOF
gc init
run 2a-preview-format bro preview format $E --diff=$L/$e-2a.patch
run 2b-format-diff bro format $E --diff=$L/$e-2b.patch
run 3-format bro format $E --timings
git status --short > $L/$e-3-status.txt; gc format
run 3a-format-again bro format $E --once
run 4-check bro check $E
s=$(date +%s.%N); bro check $E --json > $L/$e-4j-check.json 2> $L/$e-4j-check.err; echo "exit $? time $(echo "$(date +%s.%N) - $s" | bc)" >> $L/$e-4j-check.err
run 5-checkbuild bro check --build $E
s=$(date +%s.%N); bro check --build $E --json > $L/$e-5j-checkbuild.json 2> $L/$e-5j-checkbuild.err; echo "exit $? time $(echo "$(date +%s.%N) - $s" | bc)" >> $L/$e-5j-checkbuild.err
run 6-preview-format-clean bro preview format $E --diff=$L/$e-6.patch
run 6b-format-diff-clean bro format $E --diff=$L/$e-6b.patch
for c in Debug Release; do
  run twae-$c dotnet build ObservableCollections.slnx -c $c --no-incremental $P -p:TreatWarningsAsErrors=true
  run test-$c dotnet test ObservableCollections.slnx -c $c $P
done
git status --short > $L/$e-end-status.txt
