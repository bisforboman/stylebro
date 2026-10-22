### New

- `bro check` and `bro format` take `--engine host` (experimental): their checks run inside the `bro` process with the
  SDK's own `dotnet format` code, compiler and analyzers, loading each configuration's projects once instead of starting
  `dotnet format` for every target framework, configuration and pass. Fixes still run in `dotnet format`. The default is
  unchanged. See [The in-process engine](https://bisforboman.github.io/stylebro/format-engine/).
