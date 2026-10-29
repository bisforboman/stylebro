SRC=/home/user/cand61/fluentscheduler-FluentScheduler; COMMIT=04f64f85dd88aaa2681789167e201a8db58df716
SLN=FluentScheduler.sln
TESTS='dotnet test FluentScheduler.sln -c $CFG'
PREVIEW=init
SETUP='printf "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"${V:-0.8.0-trial61.1}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n" > Directory.Build.props && git add -N Directory.Build.props && git diff --stat && bro init --write'
