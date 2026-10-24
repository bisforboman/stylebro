SLN=Esprima.sln
TESTS='dotnet test test/Esprima.Tests -c $CFG -f net8.0; a=$?; dotnet test test/Esprima.Tests.SourceGenerators -c $CFG; b=$?; exit $((a|b))'
PREVIEW=init
SETUP='sed -i "s#^\(\s*\)<PackageVersion Include=\"PolySharp\".*#&\n\1<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial51.2\" />#" Directory.Packages.props && sed -i "s#^</Project>#  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />\n  </ItemGroup>\n\n</Project>#" Directory.Build.props && git diff --stat && bro init --write'
