# An operand longer than the line keeps the usual layout.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (operandLine.Length > width)'
    Replace = 'if (false)'
    Tests = 'FullyQualifiedName~NestedIfTests.WithCSharpier'
}
