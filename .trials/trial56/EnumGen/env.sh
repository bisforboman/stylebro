SRC=/home/user/cand/andrewlock-NetEscapades.EnumGenerators; COMMIT=d3995b8fa8854357c902e2d2b4495ed16c5f8546
SLN=NetEscapades.EnumGenerators.sln
TESTS='dotnet test NetEscapades.EnumGenerators.sln -c $CFG'
PREVIEW=init
SETUP='for f in Directory.Build.props build/Directory.Build.props tests/NetEscapades.EnumGenerators.Benchmarks/Directory.Build.props; do python3 /home/user/stylebro/.trials/trial56/addref.py $f "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial56.1\" PrivateAssets=\"all\" />"; done && git diff --stat && bro init --write'
