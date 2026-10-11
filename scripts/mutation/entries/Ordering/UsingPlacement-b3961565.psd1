# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'if (lines[firstLine].Start + indentation.Length != first.SpanStart'
    Replace = 'if (false'
    Tests = 'UsingPlacementTests'
}
