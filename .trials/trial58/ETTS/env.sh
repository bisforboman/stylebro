SRC=/home/user/cand58/zspitz-ExpressionTreeToString; COMMIT=ca6e099c1f92a1dbe152d95484da75f6eafd157b
SLN=ExpressionTreeToString.sln
TESTS='dotnet test Tests/Tests.csproj -c $CFG -f net6.0'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial58.1\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && cat Directory.Build.props && bro init --write'
