SRC=/home/user/cand64/serilog-serilog-expressions; COMMIT=697c8fa89ec167b1f3f9457c8cee500ad02e8bc2
SLN=serilog-expressions.sln
# global.json pins 9.0.200 (prep lowers it); library net9.0..netstandard2.0 (+ net471/net462 on Windows); tests net9.0 (roll forward)
TESTS='dotnet test serilog-expressions.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial64/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial64.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
