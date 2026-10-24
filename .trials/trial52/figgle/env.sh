SLN=Figgle.slnx
TESTS='dotnet test Figgle.slnx -c $CFG'
PREVIEW=init
SETUP='for f in src/Directory.Build.props tests/Directory.Build.props; do sed -i "0,/^\s*<ItemGroup>/s##  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />#" $f; done && sed -i "0,/^\s*<ItemGroup[^>]*>/s##&\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial52.2\" />#" Directory.Packages.props && git diff && bro init --write'
