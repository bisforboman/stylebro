# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '|| moved.Any(u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))'
    Replace = ''
    Tests = 'UsingPlacementTests'
}
