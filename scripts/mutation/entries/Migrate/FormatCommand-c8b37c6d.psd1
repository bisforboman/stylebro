# Trial 59: a kept finding's first use moves to where the formatted text has it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return now.Text is null ? kept : new KeptFinding('
    Replace = 'return now.Text is not null ? kept : new KeptFinding('
    Tests = 'MigrationTests.AKeptFindingsFirstUse_IsWhereTheFormattedTextHasIt'
}
