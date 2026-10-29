SRC=/home/user/cand62/serilog-serilog-settings-configuration; COMMIT=df51082e11ceb7cc9fbc2bff15008b946ecdc616
SLN=serilog-settings-configuration.sln
TESTS='dotnet test serilog-settings-configuration.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial62/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial62.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
