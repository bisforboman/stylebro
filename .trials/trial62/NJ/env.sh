SRC=/home/user/cand62/ninject-Ninject; COMMIT=01c86efb64f551e1f6a043ef2d5c03a2612918dd
SLN=Ninject.sln
# net462 test runs need mono: net6.0 only; ModuleLoaderTests...PassesMatchingFilesToAppropriatePlugin fails untouched
TESTS='dotnet test Ninject.sln -c $CFG -f net6.0'
PREVIEW=migrate
SETUP='bro migrate . --write && sed -i "s#<PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\">#<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial62.1}\">#" src/Ninject/Ninject.csproj && git diff --stat'
