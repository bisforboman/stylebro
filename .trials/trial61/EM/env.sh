SRC=/home/user/cand61/mganss-ExcelMapper; COMMIT=035a2bc3acd6d1046a0bb244e85ef579c7116724
SLN=ExcelMapper.sln
TESTS='dotnet test ExcelMapper.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial61.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
