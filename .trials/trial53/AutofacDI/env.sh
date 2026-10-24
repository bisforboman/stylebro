SRC=/home/user/cand/autofac-Autofac.Extensions.DependencyInjection; COMMIT=206de12cc40baa58cc6f807e005f99d2e44c1ae4
SLN=Autofac.Extensions.DependencyInjection.sln
TESTS='dotnet test Autofac.Extensions.DependencyInjection.sln -c $CFG'
PREVIEW='migrate .'
SETUP='bro migrate . --write && git ls-files "*.csproj" | xargs sed -i "s#<PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\">#<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial53.1\">#" && git diff --stat'
