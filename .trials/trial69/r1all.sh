cd /home/user/stylebro/.trials/trial69
export DOTNET_ROLL_FORWARD=Major
for spec in "azure-cost-cli src/azure-cost-cli.sln" "dotliquid src/DotLiquid.sln" "NModbus NModbus.sln" "Arch Arch.sln"; do
  set -- $spec; echo "=== $1"; timeout 3000 bash run.sh $1 $2 r1 init; cat $1/logs/r1-summary.txt
done
echo ALLDONE
