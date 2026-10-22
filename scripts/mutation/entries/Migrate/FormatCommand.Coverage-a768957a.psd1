# The xUnit gap is a note too (trial 44)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = 'gaps.Where(g => !g.EndsWith(NoteEnd, StringComparison.Ordinal))'
    Replace = 'gaps.Where(g => !g.EndsWith(HeldBackReason, StringComparison.Ordinal))'
    Tests = 'MigrationTests.XunitGap_IsANote'
}
