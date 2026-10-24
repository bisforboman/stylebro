SLN=Nito.AsyncEx.sln
TESTS='dotnet test Nito.AsyncEx.sln -c $CFG'
PREVIEW=init
SETUP='for f in src/Directory.Build.props test/Directory.Build.props; do sed -i "0,/^\s*<ItemGroup>/s##  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial51.2\" PrivateAssets=\"all\" />#" $f; done && git diff && bro init --write'
