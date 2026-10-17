# BRO1001: a property BRO1527 puts on one line wants no BRO1519 blank line.
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '            && !(autoAccessorLines is not null && previous is PropertyDeclarationSyntax property && Layout.AutoAccessorLines.BecomesOneLine(property, text, autoAccessorLines))'
    Replace = ''
    Tests = 'FixOrderTests.AutoAccessors_SortedAboveANewNeighbourWithoutBro1505'
}
