# Call chain layout (BRO1523, Roslynator RCS0054) (2026-10-04)

## Question

[beyond-stylecop.md](../beyond-stylecop.md) #4: when a call chain is split over several lines, every call starts its own
line. Which chains count, what may stay on the chain's first line, what is a "call", and which indentation do new lines
get?

Facts: Roslynator's [RCS0054](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0054) (off by default;
[analyzer](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers/CSharp/FixFormattingOfCallChainAnalyzer.cs),
[fix](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers.CodeFixes/CSharp/CodeFixHelpers.cs))
starts at the outermost invocation, element access or conditional access and walks every `.` and `?.` back from the
end. Each must start a line indented one unit deeper than the chain's first line, until it reaches one on the chain's
first line; so the first line may hold anything, a property step is split too (`.WriteTo` + newline + `.Sink(x)`), and
a chain is reported as soon as any `.` is off its first line, also when only the arguments span lines
(`Task.Run(() => { ... }).ConfigureAwait(false)`). Its fix re-indents the lines inside a moved step (lambda bodies).

Survey of the eight reference repositories (scripts/realworld/repos.psd1, default branches, 2026-10-04), chains with two
or more `.`/`?.` and at least one call, outside interpolated strings: 42,705; split (a `.` or `?.` starts a line):
20,959 (Jellyfin 17,215, Polly 2,179, OpenTelemetry 922, FFMpegCore 248, FluentValidation 189, Serilog 182, CsvHelper 13,
Newtonsoft.Json 11).

- First line of the split chains: the receiver only (`people` + newline + `.Where(...)`) 2,652; the receiver and one
  call (`services.AddA()` + newline + `.AddB()`) 17,800; two or more calls 58 (mostly FluentValidation's
  `RuleFor(x => x.Name).NotNull()` + newline + `.WithMessage(...)`).
- Already one call per line below the first line (this rule as chosen): 20,880 (99.6%); 79 chains, 110 calls, 45 files
  would change (Jellyfin 49 calls, FluentValidation 22, CsvHelper 12, Polly 12, OpenTelemetry 10, Newtonsoft.Json 4,
  Serilog 1, FFMpegCore 0). Typical: `.Cast<T>().Single()`, `.GreaterThan(100).WithName("Foo")`, CsvHelper's
  `.Map(m => m.A).Name("A1").Default("WEW")`.
- RCS0054's view (every `.` off the first line starts a line): 711 split chains differ, mostly property steps (Serilog
  142 of 182: `.WriteTo.Sink(...)`, `.MinimumLevel.Debug()`); plus 404 chains that aren't split but have a `.` after
  multi-line arguments.
- A property after the last call (`.OutputToFile(...).Arguments`, `.Result`): 46 chains keep it on the call's line
  (FFMpegCore 38).
- Indentation of the lines that start with `.`, against the chain's first line (20,510 chains whose breaks all sit
  before a call step): one unit (4 columns) 19,887; aligned elsewhere, usually under the first `.` 516 (Polly 443,
  OpenTelemetry 46); 0 columns 55; 8 columns 27; 2 columns 12; mixed within the chain 13.
- No call that would move has a comment or a directive in its gap; 2 multi-line steps have lines less indented than
  their new line would be.

## Choices

1. **Which chains:** only split chains (a `.`/`?.` starts a line), like BRO1108 for lists (never split a one-line chain);
   or also chains where only the arguments span lines, like RCS0054 (404 more chains, e.g. every
   `Task.Run(() => { ... }).ConfigureAwait(false)` would be split).
2. **The first line:** free, any number of calls (RCS0054); or the receiver and at most one call (50 more chains, mostly
   FluentValidation tests; and `RuleFor(x).NotNull()` would be fine while `validator.RuleFor(x).NotNull()` wouldn't).
3. **What starts a line:** every `.` (RCS0054: Serilog's `.WriteTo` + newline + `.Sink(x)`); or each call step: the members
   from the one after a call up to and including the next call (`.WriteTo.Sink(x)` stays together), with a property
   after the last call left alone.
4. **Indentation:** enforce one unit deeper than the chain's first line from `.editorconfig` (RCS0054; Polly's 443
   aligned chains would be re-indented, and moved steps with lambda bodies need re-indenting); or don't check it and give
   new lines the indentation of the chain's own lines that already start with `.`.

## Answer

Defaults chosen from the survey (agent's proposal, 2026-10-04, for the owner to review): split chains only; the first
line free; call steps (a member that only leads to the next call stays with it, a trailing property stays on its call's
line); indentation not checked, new lines copy the chain's own. That reports 110 calls in 79 chains across the eight
repositories, each a call tacked onto a line where another call already sits. Only the gap before the `.` is rewritten;
a multi-line step moves only when its other lines are at least as deep as its new line, so argument contents never
change. Skipped: comments in the gap, directives anywhere in the chain, interpolated strings, syntax errors. Warning in
the preset; off after `stylebro-migrate` (no StyleCop rule asks for it). An indentation option (choice 4, RCS0054's)
could come later as a separate setting if someone asks.
