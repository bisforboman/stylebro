SLN=Pipelines.Sockets.Unofficial.sln
TESTS='dotnet test tests/Pipelines.Sockets.Unofficial.Tests -c $CFG -f net8.0; a=$?; dotnet test tests/Pipelines.Sockets.Unofficial.Tests -c $CFG -f net10.0; b=$?; exit $((a|b))'
PREVIEW=init
SETUP='sed -i "s#^\(\s*\)<PackageReference Include=\"Microsoft.SourceLink.GitHub\".*#&\n\1<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial51.2\" PrivateAssets=\"all\" />#" Directory.Build.props && grep -n StyleBro Directory.Build.props && bro init --write'
