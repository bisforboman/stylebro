SRC=/home/user/cand62/rebus-org-Rebus; COMMIT=8600c812c9909c0a21906d8da734639b89478f30
SLN=Rebus8.sln
TESTS='dotnet test Rebus.Tests -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial62.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
