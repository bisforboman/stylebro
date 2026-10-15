# Trial 23: only snapshot and approval files
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'if (document.FilePath is { } path && IsSnapshotFile(path)'
    Replace = 'if (document.FilePath is { } path'
    Tests = 'KeptFindingsTests.APropertyInASnapshotFile_IsKept'
}
