# An operand over several lines keeps the usual layout.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (parts.Any(p => p.IndexOf(''\n'') >= 0))'
    Replace = 'if (false)'
    Tests = 'NestedIfTests.WithCSharpier'
}
