#!/bin/bash
cd /home/user/cand/new
S=/home/user/stylebro/.trials/trial54/smoke.sh
$S ZiggyCreatures-FusionCache ZiggyCreatures.FusionCache.slnx
$S microsoft-FeatureManagement-Dotnet Microsoft.FeatureManagement.sln
$S ninject-Ninject Ninject.sln
$S JamesNK-Newtonsoft.Json.Schema Src/Newtonsoft.Json.Schema.slnx
$S Cysharp-ZLinq ZLinq.slnx
echo ALL-DONE > /home/user/stylebro/.trials/trial54/logs/smoke-done
