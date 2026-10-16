# Trial 24 (Refit): kinds joined by | share a place (SST1201)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'foreach (var name in part.Split(''|''))'
    Replace = 'foreach (var name in new[] { part })'
    Tests = 'MemberOrderingTests.GroupedKinds'
}
