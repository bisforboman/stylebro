# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'
    Find = '|| !IsPlainGap(before, clause.WhereKeyword))'
    Replace = ')'
    Tests = 'ConstraintPlacementTests'
}
