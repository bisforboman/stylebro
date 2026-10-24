SLN=Source/EasyNetQ.slnx
TESTS='for p in EasyNetQ.Tests EasyNetQ.Hosepipe.Tests EasyNetQ.Serialization.Tests EasyNetQ.ApprovalTests; do dotnet test Source/$p -c $CFG; done'
PREVIEW=init
SETUP='sed -i "0,/^\s*<ItemGroup/s##  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />\n  </ItemGroup>\n\n&#" Source/Directory.Build.props && sed -i "0,/^\s*<ItemGroup[^>]*>/s##&\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial52.1\" />#" Source/Directory.Packages.props && git diff && bro init --write'
