. /home/user/stylebro/.trials/trial50/Dahomey.Json/env.sh
C=$1; T=$2; N=$3; shift 3; L=/home/user/stylebro/.trials/trial50/Dahomey.Json/logs/recheck
cd $C; s=$(date +%s); "$@" > $L/$T-$N.log 2>&1; e=$?
echo "$T $N exit=$e $(( $(date +%s)-s ))s :: $*" | tee -a $L/$T-summary.txt
