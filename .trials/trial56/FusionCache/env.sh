SRC=/home/user/cand/ZiggyCreatures-FusionCache; COMMIT=af09f81a3ea8d7ed71183b46501946da801a2a22
SLN=ZiggyCreatures.FusionCache.slnx
TESTS='dotnet test --solution ZiggyCreatures.FusionCache.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial56/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial56.2\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
