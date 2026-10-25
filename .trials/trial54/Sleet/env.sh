SRC=/home/user/cand/emgarten-Sleet; COMMIT=833134d176a6781b1fa3d5f902eee49720184ccf
SLN=Sleet.slnx
TESTS='dotnet test --solution Sleet.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial54/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial54.1\" />" && python3 /home/user/stylebro/.trials/trial54/addref.py build/common.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
