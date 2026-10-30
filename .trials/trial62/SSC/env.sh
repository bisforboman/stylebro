SRC=/home/user/cand62/serilog-serilog-settings-configuration; COMMIT=df51082e11ceb7cc9fbc2bff15008b946ecdc616
# the tests run dotnet publish and read its output to the end: with MSBuild node reuse a reused node keeps the pipe open and the net9.0/net10.0 runs hang (smoke)
SLN=serilog-settings-configuration.sln
TESTS='MSBUILDDISABLENODEREUSE=1 dotnet test serilog-settings-configuration.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial62/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial62.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
