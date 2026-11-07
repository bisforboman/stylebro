source /home/user/stylebro/.trials/trial69/env.sh
export DOTNET_ROLL_FORWARD=Major
cd /home/user/stylebro/.trials/trial69/$1 || exit 1
s=$(date +%s)
timeout 1500 dotnet build $2 -c Debug -p:NuGetAudit=false -p:EnableWindowsTargeting=true > ../logs/smoke-$1.log 2>&1
echo "$1 exit $? in $(( $(date +%s) - s ))s" >> ../logs/smoke-summary.txt; grep -E " error |Warn|Error" ../logs/smoke-$1.log | sort -u | tail -6 >> ../logs/smoke-summary.txt
