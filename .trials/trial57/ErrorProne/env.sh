SRC=/home/user/cand57/sergeyteplyakov-ErrorProne.NET; COMMIT=b57da22a2751e091822637ad97ecbbdbe2900aca
SLN=src/ErrorProne.NET.sln
TESTS='cd src && dotnet test ErrorProne.NET.sln -c $CFG'
PREVIEW=init
PREP="sed -i '/dotnet-tools/d' src/NuGet.config"
SETUP='python3 /home/user/stylebro/.trials/trial57/addref.py src/Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial57.1\" />" && python3 /home/user/stylebro/.trials/trial57/addref.py src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
