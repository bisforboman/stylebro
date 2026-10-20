### Fixed

- **`bro format`** now formats the files a solution's projects compile from elsewhere in the repository, above the
  solution's folder (`<Compile Include="..\..\lib\*.cs" />` in `src/Tools/Tools.csproj`). They were left out without a word,
  `bro check` called the rest clean, and a build with warnings as errors failed on their findings.
- **`bro preview`** (`--diff`) now ends like `bro format` when part of the build wasn't checked: the `Not checked:` lines,
  "Not verified" and exit code 3. It said "clean after 1 run" and exited with 0.
