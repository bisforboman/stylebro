SRC=/home/user/cand/microsoft-FeatureManagement-Dotnet; COMMIT=787f819938ce6893452c04ff3088b11f54943a61
SLN=Microsoft.FeatureManagement.sln
TESTS='dotnet test Microsoft.FeatureManagement.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial55/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial55.1\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
