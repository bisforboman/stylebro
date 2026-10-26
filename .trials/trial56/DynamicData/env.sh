SRC=/home/user/cand/reactiveui-DynamicData; COMMIT=65016e5ba22a7c889b0a06cc1094e389281612c3
SLN=src/DynamicData.sln
TESTS='dotnet test src/DynamicData.sln -c $CFG'
PREVIEW='migrate .'
SETUP='bro migrate . --write && sed -i "s#<GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />#<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial56.2\" />#" src/Directory.Packages.props && git diff --stat'
