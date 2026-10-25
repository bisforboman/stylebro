SRC=/home/user/cand/tmenier-Flurl; COMMIT=36a3fa2b7e29ba1e44a39edfd0d4ae36284a6c75
SLN=Flurl.sln
TESTS='dotnet test test/Flurl.Test/Flurl.Test.csproj -c $CFG -f net6.0'
PREVIEW=init
SETUP='for p in src/*/*.csproj test/*/*.csproj; do python3 /home/user/stylebro/.trials/trial54/addref.py $p "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial54.1\" PrivateAssets=\"all\" />"; done && git diff --stat && bro init --write'
