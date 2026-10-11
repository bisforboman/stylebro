# BRO1150 on lists and arrays when Sonar wants Exists/Find (2026-10-10)

## Question

In the 0.4.0-alpha.1 trial (RealWorld, SonarAnalyzer.CSharp 9.19), BRO1150 turned `list.Where(p).Any()` into
`list.Any(p)`, which Sonar's S6605 then reported ("use Exists"), and `FirstOrDefault(p)` got S6602 ("use Find"); with
TreatWarningsAsErrors the fix broke the build. What should BRO1150 write on a `List<T>` or an array?

## Choices

1. The collection's own method where Sonar's rule is on: `list.Exists(p)`, `list.Find(p)`, `Array.Exists(items, p)`,
   `Array.Find(items, p)`; the LINQ call everywhere else.
2. Always the LINQ call (Sonar's warning stays for a person).
3. Skip lists and arrays where Sonar's rule is on.

## Decision

1 (owner). "On" means a severity configured for S6605/S6602 (.editorconfig, global config, ruleset), or, without one,
the default of the SonarAnalyzer.CSharp package the project uses: probed with the real package, both are on by default
before 10.0 (9.19, 9.32) and off from 10.3 on (10.3 to 10.35). The package's build targets pass Sonar's analyzer path
as `build_property.StyleBroSonar`, which carries the version. Only `Any` and `FirstOrDefault` change (`All`, S6603's
case, is never written by BRO1150), and only where the new call binds to the collection's method (a `Func` predicate
isn't a `Predicate`; `Array` needs `using System;`).
