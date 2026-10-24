SLN=src/Oakton.sln
TESTS='dotnet test src/Tests/Tests.csproj -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial51.2\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && bro init --write'
