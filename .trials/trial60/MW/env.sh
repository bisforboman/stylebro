SRC=/home/user/cand60/khellang-Middleware; COMMIT=f31305e1dcfe865501a3daa3d07d037063ddcb7c; SUBMODULES=1
SLN=Hellang.Middleware.sln
TESTS='dotnet test Hellang.Middleware.sln -c $CFG'
PREVIEW=init
PREP='sed -i "/dotnetfeed.blob.core.windows.net/d" NuGet.config'
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
# After the naive run (logs/naive-setup-format.log): format named src/ and test/Directory.Build.props; follow it.
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && for p in src test; do python3 /home/user/stylebro/.trials/trial60/addref.py $p/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />"; done && git diff --stat && bro init --write'
