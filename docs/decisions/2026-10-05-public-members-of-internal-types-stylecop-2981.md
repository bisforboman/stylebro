# Public members of internal types (StyleCop #2981) (2026-10-05)

## Question

StyleCop issue [#2981](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2981) asks for `internal` instead of
`public` on members of an internal type. It was rejected in [beyond-stylecop.md](../beyond-stylecop.md) (#11): the rewrite
can change run-time behavior. The owner first questioned whether reflection cares about public vs private at all. It
does: reflection's defaults (`GetMethods()`, `GetProperties()`, `BindingFlags.Public`) and the libraries built on them
(System.Text.Json, Newtonsoft.Json, XmlSerializer, data binding, model binding, AutoMapper, Dapper, `Activator`,
dependency injection, test frameworks) see public members only.

## Choices

- Keep it rejected.
- A narrow variant, off by default: ordinary methods only, everything reflection, frameworks or the compiler look for
  left out.
- The full rule (every member kind).

## Decision

The narrow variant, off by default (owner): **BRO1409**, because reflection's and serializers' defaults see public
members only. Agent's choices: properties, fields, events, indexers, constructors, operators, finalizers and nested
types aren't reported; neither are overrides, `virtual`/`abstract`, interface implementations (also an inherited method a
derived class implements an interface with), methods of interfaces, attributed methods, methods of attributed types or of
types with a base type from another assembly (other than `object`/`ValueType`), convention names (`Main`, `Dispose`,
`GetEnumerator`, `GetAwaiter`, `Add`, `Deconstruct`, `Configure`, `Invoke`, record members, ...), `extern`/`partial`
methods, and `#if` in the method or a type header. Extension methods are reported (`internal` works for them). A name
that is a string literal or a `nameof` anywhere in the solution is reported but not fixed (like the naming rules'
reflection guard), and so is a method of a type another project (`InternalsVisibleTo`) derives from. Off after
`stylebro-migrate` (no StyleCop rule).
