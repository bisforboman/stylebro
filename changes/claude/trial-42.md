### Fixed

- **File-based programs** (`dotnet run app.cs`, .NET 10) no longer get StyleBro's analyzers from a repository's
  Directory.Build.props: `dotnet format` can't load them, so `bro format` couldn't fix what the build then reported (a
  build that runs such a script failed with warnings as errors).
- **BRO1151** leaves an array passed to a C# 14 extension member's `params` parameter alone: the compiler reports CS8620
  for the elements passed one by one, which broke builds with warnings as errors.
- **bro** reads projects whose evaluation fails on an item condition outside a build (a version comparison inside a
  group for .NET Framework only, MSB4184): `init`, `format` and the preview stopped at their first MSBuild query there.
- **bro preview** names the configurations an in-solution source generator keeps from the runs, as `bro format` does,
  and ends with "Not verified" for them.
