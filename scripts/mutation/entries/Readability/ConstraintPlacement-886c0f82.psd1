# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'
    Find = 'return joins.Count > 0 && maxLength != int.MaxValue'
    Replace = 'return joins.Count > 0 && false'
    Tests = 'ConstraintPlacementTests'
}
