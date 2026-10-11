# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'fromContainers.Count > 0 ? !fromContainers.SetEquals(fromU) :'
    Replace = 'fromContainers.Count > 0 ? false :'
    Tests = 'UsingPlacementTests'
}
