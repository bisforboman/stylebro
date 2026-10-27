#!/bin/bash
# Smoke build: restore + Debug build + tests of each candidate (trial 57 picks), one at a time.
export NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 CI=true DiffEngine_Disabled=true
cd /home/user/stylebro/.trials/trial57/probe
lower() { python3 -I - "$1" <<'EOF'
import json, sys
p = sys.argv[1]; d = json.load(open(p)); d['sdk']['version'] = '10.0.100'; d['sdk']['rollForward'] = 'latestFeature'
json.dump(d, open(p, 'w'), indent=2)
EOF
}
run() { name=$1; shift; echo "== $name: $* ($(date +%T))"; ( "$@" ) > logs/$name.log 2>&1; echo "   exit $? ($(date +%T)) $(grep -c ' error ' logs/$name.log) error lines"; }
mkdir -p logs
lower Newtonsoft.Json.Bson/Src/global.json; lower OpenAPI.NET/global.json; lower ErrorProne.NET/src/global.json
run bson-build dotnet build Newtonsoft.Json.Bson/Src/Newtonsoft.Json.Bson.slnx -p:NuGetAudit=false
run bson-test dotnet test Newtonsoft.Json.Bson/Src/Newtonsoft.Json.Bson.slnx --no-build
run openapi-build dotnet build OpenAPI.NET/Microsoft.OpenApi.slnx -p:NuGetAudit=false
run openapi-test dotnet test --solution OpenAPI.NET/Microsoft.OpenApi.slnx --no-build
run errorprone-build dotnet build ErrorProne.NET/src/ErrorProne.NET.sln -p:NuGetAudit=false
run errorprone-test dotnet test ErrorProne.NET/src/ErrorProne.NET.sln --no-build
run mapster-build dotnet build Mapster/src/Mapster.sln -p:NuGetAudit=false
run mapster-test dotnet test Mapster/src/Mapster.sln --no-build
echo done
