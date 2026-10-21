# Trial 38: a listing's first type may carry attributes
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '(?:\[[^\r\n]*\]\s*\r?\n\s+)*public'
    Replace = 'public'
    Tests = 'KeptFindingsTests.IsApiListing'
}
