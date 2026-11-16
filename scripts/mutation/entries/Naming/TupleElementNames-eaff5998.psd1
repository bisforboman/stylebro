# BRO1311: a literal compared with a declared tuple follows its names (CS8383)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = '            ITupleBinaryOperation comparison => ComparedType(comparison.LeftOperand == operation ? comparison.RightOperand : comparison.LeftOperand),'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_ThatFollowADeclaredTupleTypeThatKeepsItsNames'
}
