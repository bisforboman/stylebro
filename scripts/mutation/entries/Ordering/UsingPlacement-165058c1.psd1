# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '+ (IsBlank(next) || nextIsClose ? string.Empty : lineBreak);'
    Replace = ';'
    Tests = 'UsingPlacementTests'
}
