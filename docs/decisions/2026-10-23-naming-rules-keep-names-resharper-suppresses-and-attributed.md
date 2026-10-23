# Naming rules keep names ReSharper suppresses and attributed parameters (2026-10-23)

## Question

Trial 49, mayuki/Cocona: BRO1302 renamed `CommandName(bool dryRun, [Argument] string ArgName0)` to `argName0`, and 5 tests
(on both frameworks) failed: Cocona reads parameter names by reflection, as the command line's argument names. The line
above the method says `// ReSharper disable once InconsistentNaming`. Real command methods (Cocona's `[Argument]`/`[Option]`,
ASP.NET's `[FromQuery]`) bind the same way. How should the naming rules treat this?

## Choices

1. **Both signals** (owner's pick): a parameter with an attribute of its own is a kept finding ("frameworks bind such
   names"), listed and baselined like the nameof case; and a name where ReSharper's InconsistentNaming is suppressed
   (`disable once` above it, or `disable` up to `restore`) isn't reported by the naming rules at all. Caller-info,
   nullability and interop attributes don't count as binding.
2. Attribute only.
3. ReSharper comments only (attributed parameters still renamed: `[Argument] string Argument` -> `argument` compiles and
   passes the tests but changes the command line).

## Decision

Both: `NamingRules.IsDisabledByReSharper` (in `AllowsRename`, so every naming rule, BRO1301-BRO1314) and
`KeptReason.ParameterHasAttribute` (`CamelCaseRenamer.HasBindingAttribute`: any attribute outside
System.Runtime.CompilerServices, System.Diagnostics.CodeAnalysis, System.Runtime.InteropServices and JetBrains.Annotations).
