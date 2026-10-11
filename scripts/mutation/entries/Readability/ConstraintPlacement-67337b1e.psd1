# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'
    Find = '+ length > maxLength ? [] : joins;'
    Replace = '+ length >= maxLength ? [] : joins;'
    Tests = 'ConstraintPlacementTests'
}
