# Which strings keep a field rename (2026-10-11)

## Question

The renaming fix kept a field's old name when that name was a string literal anywhere in the solution (or, through the
repository-names file, anywhere in the repository), because another project may read the field by reflection (Polly's
tests: `GetField("_blockedUntil")`). A trial on four new repositories showed that this is far too broad: in Orleans 692
private field renames were kept because common words are strings somewhere (`Assert.Equal("options", e.ParamName)`), and
Flow.Launcher kept `private string type` because "type" is a JSON key in another plugin. Which strings should keep a
field's name?

## Choices

- Any string (as before): safe, but most of the kept renames are noise and the warnings stay forever.
- Reflection-shaped strings only: a string keeps a field's name only where code names a member to reach it by name.
- Strings in the same project only: misses the case the guard exists for (tests in another project).

## Decision

The owner: "Reflection-shaped strings only." A string keeps a (non-data) field's name when it is an argument of a method
that reaches members by name (`GetField`, `GetProperty`, `GetMember`, `GetMethod`, `GetEvent` and their `GetRuntime*`/
`GetDeclared*` forms, `Type.GetType`, `InvokeMember`, `Activator.CreateInstance`, and any method whose name contains
Field, Property, Member, Method or Event: `Expression.Field`, `AccessTools.Field`, EF Core's `HasField`), an argument of
`[MemberData]`/`[DynamicData]`/`[TestCaseSource]`/`[ValueSource]`, or any attribute's `Name = "..."`
(`[UnsafeAccessor(..., Name = "count")]`). Syntax only, so the repository-names file (`R` lines) carries them to
per-framework runs and `--files` runs. Unchanged: strings in the field's own type (the analyzer still matches the name
inside any string there, for `DebuggerDisplay`), and public instance fields, properties, events and enum members, which
serializers write by name (matched inside any string, like test JSON). Measured on Orleans: see the pull request.
