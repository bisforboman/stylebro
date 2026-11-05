source /home/user/stylebro/.trials/trial68/env.sh
cd /home/user/stylebro/.trials/trial68/$1 || exit 1
dotnet build $2 -c Debug -p:NuGetAudit=false -p:EnableWindowsTargeting=true > ../logs/smoke-$1.log 2>&1
echo "$1 exit $?" >> ../logs/smoke-summary.txt; tail -5 ../logs/smoke-$1.log >> ../logs/smoke-summary.txt
