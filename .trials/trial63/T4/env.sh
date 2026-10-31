SRC=/home/user/cand63/mono-t4; COMMIT=e128cfc9c86f4de2191cfe8ccbb58fccbe965096
SLN=Mono.TextTemplating.sln
# net472/net48 only on Windows; tests net6.0/net7.0 (roll forward to 10); Nerdbank.GitVersioning needs the full history
TESTS='dotnet test Mono.TextTemplating.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial63/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial63.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
