SRC=/home/user/cand57/JamesNK-Newtonsoft.Json.Bson; COMMIT=083ffaaf5e5bf69c3bdcbfeb0cfd986aea4dd119
SLN=Src/Newtonsoft.Json.Bson.slnx
TESTS='cd Src && dotnet test Newtonsoft.Json.Bson.slnx -c $CFG'
PREVIEW=init
PREP="sed -i '/json-net-public/d' Src/NuGet.Config"
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial57.1\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && cat Directory.Build.props && bro init --write'
