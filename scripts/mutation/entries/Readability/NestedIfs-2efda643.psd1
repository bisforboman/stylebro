# BRO1149: an || chain operand breaks inside its parentheses under CSharpier.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '            if (operands[i].Ors is not { } ors)'
    Replace = '            if (true)'
    Tests = 'FullyQualifiedName~AnOrChainOperand'
}
