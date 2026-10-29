SRC=/home/user/cand60/Cysharp-MasterMemory; COMMIT=78e151315d9eb849ca10dc0aea4d6b866b735da4
SLN=MasterMemory.sln
TESTS='dotnet test MasterMemory.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial60/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
