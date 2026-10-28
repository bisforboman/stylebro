SRC=/home/user/cand59/hazzik-DelegateDecompiler; COMMIT=bc42e9706ddc4ec19166e8512256a32047c635e9
SLN=DelegateDecompiler.sln
TESTS='dotnet test src/DelegateDecompiler.Tests/DelegateDecompiler.Tests.csproj -c $CFG -f net8.0; dotnet test src/DelegateDecompiler.Tests.VB/DelegateDecompiler.Tests.VB.vbproj -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial59/addref.py src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial59.1\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
