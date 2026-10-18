# xUnit's fixes

xunit.analyzers comes with every `xunit` and `xunit.v3` package. Where a repository references xunit (`xunit`,
`xunit.v3`, `xunit.v3.mtp-*` or `xunit.analyzers` in a project, props or targets file, or `packages.config`),
`bro format` also applies xUnit's fixes for the assertion rules below. `bro check` checks them, and `bro preview format`
shows them. `dotnet format` fixes each rule only where it is a warning or error, so the repository's own severities
decide: xunit.analyzers' default, or what `.editorconfig` sets.

| Rule | Before | After |
| --- | --- | --- |
| xUnit1014 | `[MemberData("Cases")]` | `[MemberData(nameof(Cases))]` |
| xUnit1025 | the same `[InlineData(1)]` twice | once |
| xUnit1048 | `public async void Test()` | `public async Task Test()` |
| xUnit2000 | `Assert.Equal(actual, 5)` | `Assert.Equal(5, actual)` |
| xUnit2003 | `Assert.Equal(null, x)` | `Assert.Null(x)` |
| xUnit2004 | `Assert.Equal(true, x)` | `Assert.True(x)` |
| xUnit2006 | `Assert.Equal<string>("a", s)` | `Assert.Equal("a", s)` |
| xUnit2007 | `Assert.IsType(typeof(T), x)` | `Assert.IsType<T>(x)` |
| xUnit2008 | `Assert.True(Regex.IsMatch(s, p))` | `Assert.Matches(p, s)` |
| xUnit2009 | `Assert.True(s.Contains("b"))` | `Assert.Contains("b", s)` |
| xUnit2010 | `Assert.True(string.Equals(a, b))` | `Assert.Equal(a, b)` |
| xUnit2011 | `Assert.Collection(list)` | `Assert.Empty(list)` |
| xUnit2012 | `Assert.True(list.Any(p))` | `Assert.Contains(list, p)` |
| xUnit2013 | `Assert.Equal(0, list.Count)` | `Assert.Empty(list)` |
| xUnit2015 | `Assert.Throws(typeof(E), f)` | `Assert.Throws<E>(f)` |
| xUnit2017 | `Assert.True(list.Contains(1))` | `Assert.Contains(1, list)` |
| xUnit2020 | `Assert.True(false, "message")` | `Assert.Fail("message")` |
| xUnit2029, xUnit2030 | `Assert.Empty(list.Where(p))` | `Assert.DoesNotContain(list, p)` |
| xUnit2031 | `Assert.Single(list.Where(p))` | `Assert.Single(list, p)` |

Some of these fixes leave work for another: `Assert.Equal(true, s.Contains("b"))` becomes `Assert.True(s.Contains("b"))`,
then `Assert.Contains("b", s)`. `bro format` runs again until nothing changes, so the result is the same in any order.

## Left out

The other xUnit fixes change what a test does, and stay out unless you run `bro format --all` or plain `dotnet format`:

- fixes that add or remove tests or change how they run: xUnit1013 (marks a public method as a test: on one trial
  repository it put `[Theory]` on an `ILogger.Log` implementation), xUnit1030 (removes `ConfigureAwait(false)`: a test
  that needed to lose its synchronization context failed), xUnit1051 (passes `TestContext.Current.CancellationToken`),
  xUnit1004 (removes `Skip`);
- fixes that change a parameter's type or a test's data: xUnit1006, xUnit1008, xUnit1012, xUnit1021, xUnit1034;
- rules with two fixes to choose from: xUnit1001, xUnit1005;
- fixes that turn an assertion that always fails into one that passes: xUnit2002, xUnit2005, xUnit2018;
- fixes without Fix All, and xUnit's errors (a repository that builds has none).

`bro init` writes no xUnit settings. A [baseline](baseline.md) covers the rules above, like StyleBro's.

## With StyleBro's rules

StyleBro's renames leave a member whose name is in a string, such as `[MemberData("cases")]`, because xUnit looks the
member up by that name at run time. Once xUnit1014 has written `[MemberData(nameof(cases))]`, the rename changes both and
goes ahead.
