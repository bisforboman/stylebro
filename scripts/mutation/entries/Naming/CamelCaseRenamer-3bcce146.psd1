# Trial 23 (Verify): a name in a snapshot file keeps it
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'names.Snapshots.TryGetValue(symbol.Name, out var snapshot) ?'
    Replace = 'false && names.Snapshots.TryGetValue(symbol.Name, out var snapshot) ?'
    Tests = 'KeptFindingsTests.APropertyInASnapshotFile_IsKept'
}
