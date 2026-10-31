SRC=/home/user/cand63/nlog-NLog.Extensions.Logging; COMMIT=15091c8eaeb3922f17fad1268ea1ea2f34bb89c1
SLN=NLog.Extensions.Logging.sln
# tests net10.0;net8.0;net462 (net462 needs mono: left out); .editorconfig end_of_line = crlf, LF checkout
TESTS='dotnet test NLog.Extensions.Logging.sln -c $CFG -f net8.0 && dotnet test NLog.Extensions.Logging.sln -c $CFG -f net10.0'
PREVIEW=init
# step 1 as the preview does it: the root Directory.Build.props (new) and src/Directory.Build.props (src's projects don't import the root one)
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial63.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && python3 /home/user/stylebro/.trials/trial63/addref.py src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial63.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
