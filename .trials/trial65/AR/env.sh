SRC=/home/user/cand65/stefanprodan-AspNetCoreRateLimit; COMMIT=3a574a8e6a6ab1bfe3ecf285490568c08cf1aa65
SLN=AspNetCoreRateLimit.sln
TESTS='dotnet test AspNetCoreRateLimit.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial65.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
