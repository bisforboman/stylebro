# Trial 23: only data members are kept for a snapshot word
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return IsDataSymbol(symbol) && names.Snapshots'
    Replace = 'return names.Snapshots'
    Tests = 'KeptFindingsTests.APrivateFieldInASnapshotFile_IsRenamed'
}
