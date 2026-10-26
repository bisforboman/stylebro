SRC=/home/user/cand/andrewlock-NetEscapades.EnumGenerators; COMMIT=d3995b8fa8854357c902e2d2b4495ed16c5f8546
SLN=NetEscapades.EnumGenerators.sln
TESTS='dotnet test NetEscapades.EnumGenerators.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial56/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial56.1\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
