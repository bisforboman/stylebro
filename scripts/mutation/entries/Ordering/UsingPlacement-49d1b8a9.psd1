# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'if (below > endLine && below < lines.Count && !IsBlank(lines[below]))'
    Replace = 'if (false)'
    Tests = 'UsingPlacementTests'
}
