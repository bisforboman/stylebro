SRC=/home/user/cand63/serilog-serilog-sinks-file; COMMIT=4c8e6772881a8d9c8f1b3142a753ea37e8ad2342
SLN=serilog-sinks-file.sln
# global.json pins 9.0.200 (prep lowers it to 10.0.100); tests net8.0 + net9.0 (net9.0 rolls forward to 10)
TESTS='dotnet test serilog-sinks-file.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial63/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial63.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
