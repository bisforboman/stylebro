# Trial 9: IDE0055 lowered to suggestion when git contradicts end_of_line
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '> Severity.Suggestion)'
    Replace = '> Severity.Error)'
    Tests = 'MigrationTests.EndOfLine_ThatGitsCheckoutContradicts'
}
