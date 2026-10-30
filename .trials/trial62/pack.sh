#!/bin/bash
# pack.sh <version> <tooldir>: StyleBro.Package + StyleBro.Tool into /home/user/feed62, the tool installed into <tooldir>
set -e; V=$1; TD=$2; cd /home/user/stylebro
export NUGET_CERT_REVOCATION_MODE=offline
dotnet pack src/StyleBro.Package -c Release -p:Version=$V -o /home/user/feed62
dotnet pack src/StyleBro.Migrate -c Release -p:Version=$V -o /home/user/feed62
rm -rf $TD; dotnet tool install StyleBro.Tool --version $V --tool-path $TD --add-source /home/user/feed62
$TD/bro --version || true
