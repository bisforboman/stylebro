SRC=/home/user/cand60/dotnetcore-EasyCaching; COMMIT=0ce6bd0628e431463aa22d0fe8f503e2cfa32346
SLN=EasyCaching.sln
TESTS='dotnet test test/EasyCaching.UnitTests/EasyCaching.UnitTests.csproj -c $CFG --filter "$EC_FILTER"'
PREVIEW=init
EC_FILTER='FullyQualifiedName!~Redis&FullyQualifiedName!~Memcached&FullyQualifiedName!~Hybrid&FullyQualifiedName!~ProviderFactoryTests&FullyQualifiedName!~Etcd&FullyQualifiedName!~Bus'; export EC_FILTER
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
