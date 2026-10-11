# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'
    Find = 'out var value) && value.Trim() == "same_line";'
    Replace = 'out var value);'
    Tests = 'ConstraintPlacementTests'
}
