SRC=/home/user/cand60/apache-logging-log4net; COMMIT=86c64ae53c0dde7650f461466cf41296c7b60217
SLN=src/log4net.sln
TESTS='cd src && dotnet test log4net.sln -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial60/addref.py src/Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial60.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
# Two solutions (examples/examples.sln, src/log4net.sln): format asks for one (run 1's default, logs/default-format.log)
FMT=src/log4net.sln
PREVIEW='init --project src/log4net.sln'
