# Trial 22/23: every combination of the symbols the file tests (an '#elif' branch is in neither 'none' nor 'all')
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Enumerable.Range(0, 1 << tested.Length)'
    Replace = 'new[] { 0, (1 << tested.Length) - 1 }'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
