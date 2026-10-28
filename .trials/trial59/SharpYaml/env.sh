SRC=/home/user/cand59/xoofx-SharpYaml; COMMIT=99fb455e15457eb04e65c3f65bd14f0291c4b5d8
SLN=src/SharpYaml.slnx
TESTS='cd src && dotnet test SharpYaml.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial59/addref.py src/Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial59.2\" />" && printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > src/Directory.Build.props && git diff --stat && bro init --write'
