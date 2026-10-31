SRC=/home/user/cand64/TNG-ArchUnitNET; COMMIT=f439e0d2e9a45fe9f6a70fea1b05b7c1cbbf465b
SLN=ArchUnit.sln
# global.json pins 10.0.400 (prep lowers it); libraries netstandard2.0;net462/net472, tests net10.0 (MSTest, NUnit, TUnit, xUnit v2/v3)
TESTS='dotnet test ArchUnit.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial64.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
