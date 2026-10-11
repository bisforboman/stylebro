# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'if (!global.GetMembers(first).Any() || namespacesOfN.Any(n => n.GetMembers(first).Any()))'
    Replace = 'if (false)'
    Tests = 'UsingPlacementTests'
}
