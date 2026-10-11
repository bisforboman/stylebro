# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = '&& isOn(finding.Id))'
    Replace = ')'
    Tests = 'ConstraintPlacementTests.SameLine_WithoutBro1404AndBro1007'
}
