# Kept findings (2026-10-10): a rename the fix keeps on purpose offers no code action
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (MatchInStrings(symbol, names) is { } match)'
    Replace = 'if (MatchInStrings(symbol, names) is { } match && false)'
    Tests = 'KeptFindingsTests'
}
