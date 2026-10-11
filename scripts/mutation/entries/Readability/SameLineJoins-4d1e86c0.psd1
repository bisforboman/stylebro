# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = 'if (closeMoves && close?.NewText'
    Replace = 'if (false && close?.NewText'
    Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAfterBro1110'
}
