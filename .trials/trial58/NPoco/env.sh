SRC=/home/user/cand58/schotime-NPoco; COMMIT=61ba5d16f6a9543d9cc2c5a97a63663da299b5ea
SLN=NPoco.sln
TESTS='dotnet test NPoco.sln -c $CFG'
PREVIEW=init
PREP="sed -i 's/\"TestDBType\": 2/\"TestDBType\": 1/' test/NPoco.Tests/config.json"
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial58.1\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && cat Directory.Build.props && bro init --write'
