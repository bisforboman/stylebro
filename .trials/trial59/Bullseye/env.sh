SRC=/home/user/cand59/adamralph-bullseye; COMMIT=5fb351898d04b4c0f7c1fd542052c0b1c7c7b9c0
SLN=Bullseye.slnx
TESTS='dotnet test --solution Bullseye.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial59/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial59.1\" />" && python3 /home/user/stylebro/.trials/trial59/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && dotnet restore Bullseye.slnx -p:RestoreLockedMode=false -p:NuGetAudit=false && git diff --stat && bro init --write'
