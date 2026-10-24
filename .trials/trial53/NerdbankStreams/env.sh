SRC=/home/user/cand/AArnott-Nerdbank.Streams; COMMIT=e1743fd5d41733caa3e7a9b62a55f4226fa79a94; SUBMODULES=1
SLN=Nerdbank.Streams.slnx
TESTS='dotnet test --solution Nerdbank.Streams.slnx -c $CFG'
PREVIEW='migrate .'
SETUP='bro migrate . --write && sed -i "s#<GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.3.149-beta\" />#<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial53.1\" />#" Directory.Packages.props && git diff --stat'
