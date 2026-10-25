SRC=/home/user/cand/StackExchange-Dapper; COMMIT=eb47546a408bbf6c178bda4f04dc205bc51ffbfe
SLN=Dapper.slnx
TESTS='dotnet test tests/Dapper.Tests/Dapper.Tests.csproj -c $CFG -f net10.0 --filter "FullyQualifiedName~Sqlite|FullyQualifiedName~DuckDB"'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial54/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial54.2\" />" && python3 /home/user/stylebro/.trials/trial54/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
