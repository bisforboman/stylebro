# Owner 2026-10-22 (Note it, don't fail): a held-back SDK rule doesn't count for the verdict
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = 'gaps.Where(g => !g.EndsWith(NoteEnd, StringComparison.Ordinal))'
    Replace = 'gaps.Where(g => true)'
    Tests = 'FormatCoverageTests.HeldBackSdkRules'
}
