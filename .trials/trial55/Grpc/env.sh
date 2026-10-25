SRC=/home/user/cand/protobuf-net-protobuf-net.Grpc; COMMIT=3471179ce47470ed8858c9f5cb69314c5e00dbcc
export EnableWindowsTargeting=true
SLN=protobuf-net.Grpc.slnx
TESTS='dotnet test protobuf-net.Grpc.slnx -c $CFG'
PREVIEW=init
SETUP='python3 /home/user/stylebro/.trials/trial55/addref.py Directory.Packages.props "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial55.1\" />" && python3 /home/user/stylebro/.trials/trial55/addref.py Directory.Build.props "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />" && git diff --stat && bro init --write'
