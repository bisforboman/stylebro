SRC=/home/user/cand61/jbevain-cecil; COMMIT=882ca5eedda1e62eb41bd5869aeb15d8f1538e51
SLN=Mono.Cecil.sln
# net40 tests need mono; ilasm-based tests fail untouched (no ilasm on Linux)
TESTS='dotnet test Mono.Cecil.sln -c $CFG -f net8.0'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial61/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial61.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
