SRC=/home/user/cand65/dynamicexpresso-DynamicExpresso; COMMIT=ad167c0f396127de6df1f0b555d2916992345713
SLN=DynamicExpresso.sln
TESTS='dotnet test test/DynamicExpresso.UnitTest -c $CFG -f net8.0 && dotnet test test/DynamicExpresso.UnitTest -c $CFG -f net9.0'
FMT=DynamicExpresso.sln
PREVIEW='init --project DynamicExpresso.sln'
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial65.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
