SRC=/home/user/cand58/seesharper-LightInject; COMMIT=b943de2168becd99e88f47a8b53b6ff44f85a7e4
SLN=LightInject.sln
TESTS='cd src/LightInject.Tests && PATH=$HOME/.dotnet/tools:$PATH dotnet test -c $CFG'
PREVIEW=migrate
SETUP='bro migrate . --write && sed -i "s#<PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\">#<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial58.1\">#" src/LightInject/LightInject.csproj && git diff --stat'
