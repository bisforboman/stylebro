SRC=/home/user/cand58/natemcmaster-CommandLineUtils; COMMIT=00c331e7359cf5e6e5ace82ede6984cb14d760bd
SLN=CommandLineUtils.sln
TESTS='dotnet test CommandLineUtils.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial58/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial58.2\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
