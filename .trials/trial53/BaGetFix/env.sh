SRC=/home/user/cand/loic-sharma-BaGet; COMMIT=5a8502ca4e3b21acd369d4a98c2529dac2d81cfe
SLN=BaGet.sln
TESTS='dotnet test BaGet.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial53/addref.py src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial53.2\" PrivateAssets=\"all\" />" && python3 /home/user/stylebro/.trials/trial53/addref.py tests/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial53.2\" PrivateAssets=\"all\" />" && git diff && bro init --write'
