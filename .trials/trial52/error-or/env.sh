SLN=ErrorOr.slnx
TESTS='dotnet test ErrorOr.slnx -c $CFG'
PREVIEW='migrate .'
SETUP='bro migrate . --write && sed -i "s#<PackageReference Include=\"StyleCop.Analyzers\">#<PackageReference Include=\"StyleBro.Analyzers\">#" Directory.Build.props && sed -i "s#<PackageVersion Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />#<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial52.1\" />#" Directory.Packages.props && git diff --stat'
