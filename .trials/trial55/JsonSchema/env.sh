SRC=/home/user/cand/JamesNK-Newtonsoft.Json.Schema; COMMIT=5456c4bffe9a0b18ecd70ee056fc43f8a19ebf7a
SLN=Src/Newtonsoft.Json.Schema.slnx
TESTS='dotnet test Src/Newtonsoft.Json.Schema.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial55/addref.py Src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial55.1\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
