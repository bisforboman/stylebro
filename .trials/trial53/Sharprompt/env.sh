SRC=/home/user/cand/shibayan-Sharprompt; COMMIT=5e17ace494e7e7a87cd0daa3e3481eead749a68f
SLN=Sharprompt.slnx
TESTS='dotnet test --solution Sharprompt.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial53/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial53.1\" />" && python3 /home/user/stylebro/.trials/trial53/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff && bro init --write'
