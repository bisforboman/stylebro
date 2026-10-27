SRC=/home/user/cand57/DapperLib-DapperAOT; COMMIT=93d99d36dd0142a07bf1b00567351f89b8dddb72
SLN=Dapper.AOT.slnx
TESTS='dotnet test test/Dapper.AOT.Test/Dapper.AOT.Test.csproj -c $CFG -f net10.0 --filter "FullyQualifiedName!~Integration"'
PREVIEW=init
PREBUILD_CHECK=1
SETUP='python3 /home/user/stylebro/.trials/trial57/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial57.2\" />" && python3 /home/user/stylebro/.trials/trial57/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
