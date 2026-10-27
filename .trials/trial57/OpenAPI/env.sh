SRC=/home/user/cand57/microsoft-OpenAPI.NET; COMMIT=b0a6859be48927746726e967f578f370cd756f35
SLN=Microsoft.OpenApi.slnx
TESTS='dotnet test --solution Microsoft.OpenApi.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial57/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial57.2\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
