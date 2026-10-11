# Kept findings (2026-10-10): a rename the fix keeps on purpose offers no code action
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'MatchInStrings(symbol, names) is { } match ? (KeptReason.NameInString'
    Replace = 'MatchInStrings(symbol, names) is { } match && false ? (KeptReason.NameInString'
    Tests = 'KeptFindingsTests'
}
