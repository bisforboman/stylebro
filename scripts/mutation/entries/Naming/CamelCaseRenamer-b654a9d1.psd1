# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return (itemChanges, kept, keptWhere);'
    Replace = 'continue;'
    Tests = 'LinkedFileFixAllTests'
}
