# Trial 23: only snapshot and approval files
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'return Markers.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) > 0);'
    Replace = 'return true;'
    Tests = 'KeptFindingsTests.APropertyInASnapshotFile_IsKept'
}
