# final pass at 990c775 (0.8.0-trial50.4)
set -u; D=/home/user/stylebro/.trials/trial50/Dahomey.Json; L=$D/logs/recheck3; V=0.8.0-trial50.4
. $D/env.sh
run() { C=$1; T=$2; N=$3; shift 3; cd $C; s=$(date +%s); "$@" > $L/$T-$N.log 2>&1; e=$?; echo "$T $N exit=$e $(( $(date +%s)-s ))s :: $*" | tee -a $L/summary.txt; cd $D; }
fresh() { rm -rf $D/$1; git clone -q $D/base $D/$1; git -C $D/$1 checkout -q 43ddbf14d282eccb9802a040fc5d85e7a1422166; }
addpkg() { python3 -I -c "
import sys;p=sys.argv[1];s=open(p).read()
s=s.replace('  </PropertyGroup>\n</Project>','  </PropertyGroup>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"$V\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>')
open(p,'w').write(s)" $1/Directory.Build.props; }
cd $D
for t in default host; do E=""; [ $t = host ] && E="--engine host"; C=$D/r3$t; fresh r3$t
  run $C $t preview-init bro preview init $E; cp $C/.git/stylebro-preview.patch $L/$t-preview-init.patch 2>/dev/null
  run $C $t init bro init --write; addpkg $C
  run $C $t format bro format $E
  run $C $t check bro check $E
  run $C $t check-json bash -c "bro check $E --json > $L/$t-check.json"
  run $C $t check-build bro check --build $E
  run $C $t check-build-json bash -c "bro check --build $E --json > $L/$t-check-build.json"
  for cfg in Debug Release; do run $C $t build-$cfg dotnet build src/Dahomey.Json.sln -c $cfg --no-incremental -p:TreatWarningsAsErrors=true -p:NuGetAudit=false; done
  run $C $t test dotnet test src/Dahomey.Json.sln -c Release --no-build
done
for v in p3-host p3-dotnet d3-host; do fresh $v; cp r3default/.editorconfig r3default/Directory.Build.props $v/; done
run $D/p3-host host preview-format bro preview format --engine host
run $D/p3-dotnet default preview-format-engine-dotnet bro preview format --engine dotnet-format
run $D/d3-host host format-diff bro format --diff --engine host
echo ALLDONE >> $L/summary.txt
