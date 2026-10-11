# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = 'finding.Modifier + " "'
    Replace = 'string.Empty'
    Tests = 'ConstraintPlacementTests.SameLine_NotReported_WhenAnAddedModifier'
}
