# An operand over several lines keeps the usual layout.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (value.IndexOf(''\n'') >= 0)'
    Replace = 'if (false)'
    Tests = 'FullyQualifiedName~NestedIfTests.WithCSharpier'
}
