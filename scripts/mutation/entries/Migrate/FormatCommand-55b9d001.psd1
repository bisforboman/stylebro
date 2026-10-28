# Trial 59: only a name-in-string reason has a first string to refresh
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (kept.Reason != KeptReason.NameInString || kept.Where is not { } where'
    Replace = 'if (kept.Where is not { } where'
    Tests = 'MigrationTests.AKeptFindingsFirstUse_IsWhereTheFormattedTextHasIt'
}
