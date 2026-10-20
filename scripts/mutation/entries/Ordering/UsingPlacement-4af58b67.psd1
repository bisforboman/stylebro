@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '|| (!body.Contains(disabled.SpanStart) && Regex'
    Replace = '|| (Regex'
    Tests = 'UsingPlacementTests.IfBranchDeclaringATypeInsideTheNamespace_StillMoves'
}
