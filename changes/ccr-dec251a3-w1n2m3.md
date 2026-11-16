### Fixed

- **`bro format` and `bro check`** run their default in-process engine on the .NET 11 SDK again. On SDK 11 they fell back to plain `dotnet format`, which is slower and can format a little differently.
