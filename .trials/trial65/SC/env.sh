SRC=/home/user/cand65/serilog-serilog-sinks-console; COMMIT=9b75d510787f5d54cf76584a3c3341d7eab0ee0a
SLN=serilog-sinks-console.sln
TESTS='dotnet test serilog-sinks-console.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial65/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial65.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
