SLN=src/DbUp.sln
TESTS='dotnet test src/DbUp.sln -c $CFG'
PREVIEW=init
SETUP='sed -i "0,/^\s*<\/PropertyGroup>/s##    </PropertyGroup>\n    <ItemGroup>\n        <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial52.1\" PrivateAssets=\"all\" />\n    </ItemGroup>#" src/Directory.Build.props && git diff && bro init --write'
