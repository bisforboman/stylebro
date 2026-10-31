SRC=/home/user/cand64/serilog-serilog-sinks-async; COMMIT=1ae2c3ba5809b340adac0e293494fc5c725875db
SLN=serilog-sinks-async.sln
# no Directory.Build.props or .editorconfig; library net8.0;net6.0;netstandard2.0 (+ net471/net462 on Windows), tests net6.0;net8.0 (net48 on Windows)
TESTS='dotnet test serilog-sinks-async.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial64.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
