SRC=/home/user/cand63/tonerdo-pose; COMMIT=c68ec6c120c4707f9ca4860b820162a986fda782
SLN=Pose.sln
# netstandard2.1 library, netcoreapp3.1 MSTest tests (roll forward to 10): 6 of 27 fail untouched (ILGenerator internals), 1 skipped
TESTS='dotnet test Pose.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial63.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
