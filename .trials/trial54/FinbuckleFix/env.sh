SRC=/home/user/cand/Finbuckle-Finbuckle.MultiTenant; COMMIT=3765bade0735b09fb0399802d6d592d86335441e
SLN=Finbuckle.MultiTenant.slnx
TESTS='dotnet test Finbuckle.MultiTenant.slnx -c $CFG'
PREVIEW=init
SETUP='for p in src/Directory.Build.props test/*/*.csproj samples/*/*.csproj; do python3 /home/user/stylebro/.trials/trial54/addref.py $p "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial54.2\" PrivateAssets=\"all\" />"; done && git diff --stat && bro init --write'
