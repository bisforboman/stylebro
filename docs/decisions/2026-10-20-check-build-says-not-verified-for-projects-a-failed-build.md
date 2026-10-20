# check --build says Not verified for projects a failed build kept from compiling (2026-10-20)

## Question

Trial 36 spot repro: Lib fails its TreatWarningsAsErrors build with an error in its code (CS0168) that the untouched code
has too; App references Lib, so MSBuild never compiles App and StyleBro's analyzers never run there. `bro check --build`
counted Lib's error as the untouched code's and said "Clean." with exit code 0, though App was never built (WireMock.Net
only escaped it because its failing project also had an error without a position, CS8002). What should --build say?

## Choices

1. **Not verified, naming them** (owner's pick): for each failed configuration, the projects that neither produced output
   nor got a compiler or analyzer message are listed (`Didn't compile: App`), and check ends "Not verified" (exit code 3).
2. Not clean (exit code 2).
3. Leave it: the untouched build fails too.

## Decision

Not verified, naming the projects that didn't compile.
