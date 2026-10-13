# An operand longer than the line keeps the usual layout.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (ors is null)'
    Replace = 'if (false)'
    Tests = 'NestedIfTests.WithCSharpier'
}
