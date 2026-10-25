SRC=/home/user/cand/Cysharp-ZLinq; COMMIT=f7591df24c47b7c4a262c1e78241c594fbeb7286
SLN=ZLinq.slnx
TESTS='dotnet test ZLinq.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial55/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial55.1\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
