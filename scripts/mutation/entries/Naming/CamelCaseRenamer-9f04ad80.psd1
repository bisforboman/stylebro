# Trial 23: only data members are kept for a snapshot word
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (IsDataSymbol(symbol) && names.Files.Snapshots'
    Replace = 'if (names.Files.Snapshots'
    Tests = 'KeptFindingsTests.APrivateFieldInASnapshotFile_IsRenamed'
}
