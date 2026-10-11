# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = '.Where(c => c.Span.End <= position)'
    Replace = '.Where(c => false)'
    Tests = 'ConstraintPlacementTests.SameLine_NotReported_WhenAnAddedModifier'
}
