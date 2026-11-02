SRC=/home/user/cand65/ovska-FlameCsv; COMMIT=145fad86e060192ddc7adf0ad3064ef6d1b119cf
SLN=FlameCsv.sln
# CSharpier 1.1.1 local tool (.config/dotnet-tools.json); dotnet tool restore in PREP
PREP='dotnet tool restore'
TESTS='dotnet test FlameCsv.sln -c $CFG --blame-hang-timeout 5m'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial65/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial65.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
