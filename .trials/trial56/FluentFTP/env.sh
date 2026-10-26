SRC=/home/user/cand/robinrodricks-FluentFTP; COMMIT=9fa18c533acfe53fac0222f12634fafa70879cc8
SLN=FluentFTP.sln
TESTS='dotnet test FluentFTP.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.8.0-trial56.1\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && cat Directory.Build.props && bro init --write'
