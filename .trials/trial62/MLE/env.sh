SRC=/home/user/tcand62/jbevain-mono.linq.expressions; COMMIT=7973f862ddfea552457c7b2bc457fb87c481a791
SLN=Mono.Linq.Expressions.sln
# net40 builds against /usr/lib/mono/4.0-api (a link to Microsoft.NETFramework.ReferenceAssemblies.net40 1.0.3); tests netcoreapp2.1 only (net40 needs mono)
TESTS='dotnet test Mono.Linq.Expressions.sln -c $CFG -f netcoreapp2.1'
PREVIEW=init
# Directory.Build.props is UTF-16 (addref.py keeps the encoding, as an editor would)
SETUP='python3 /home/user/stylebro/.trials/trial62/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial62.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
