SRC=/home/user/cand59/danielpalme-ReportGenerator; COMMIT=eda6e6e9ac37c76d3d3dc1514fed9cfeba26bf86
SLN=src/ReportGenerator.slnx
TESTS='cd src && dotnet test --solution ReportGenerator.slnx -c $CFG'
PREVIEW=migrate
SETUP='bro migrate . --write && sed -i "s#<PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\">#<PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial59.1\">#" src/ReportGenerator.Core/ReportGenerator.Core.csproj && git diff --stat'
