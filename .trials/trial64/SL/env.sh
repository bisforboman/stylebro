SRC=/home/user/cand64/serilog-serilog-extensions-logging; COMMIT=8de073c001421145b6adb73d2807f274d96052d3
SLN=serilog-extensions-logging.sln
# library net462..net10.0; tests net8.0;net9.0;net10.0;net48 (net48 needs mono: left out); the Benchmarks project (BenchmarkDotNet, runs real benchmarks in Release) left out
TESTS='for f in net8.0 net9.0 net10.0; do dotnet test test/Serilog.Extensions.Logging.Tests -c $CFG -f $f || r=1; done; exit ${r:-0}'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial64/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial64.1}\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
