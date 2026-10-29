SRC=/home/user/cand61/jbogard-Respawn; COMMIT=f550203c2caaded99ede64d85ff07acc6bb7514d
SLN=Respawn.sln
# Respawn.DatabaseTests need SQL Server/Postgres/MySQL/Oracle/Informix: left out
TESTS='dotnet test Respawn.UnitTests -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial61/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial61.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
