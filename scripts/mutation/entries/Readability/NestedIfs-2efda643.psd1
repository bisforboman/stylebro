# BRO1149: an || operand longer than its line keeps the usual layout.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (orLine.Length > width)'
    Replace = 'if (false)'
    Tests = 'NestedIfTests.WithCSharpier_AnOrChainOperand_BreaksInsideItsParentheses'
}
