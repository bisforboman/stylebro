# Trial 24 (Dawnshard): an EF Core migration designer
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| Regex.IsMatch(text, @"\[\s*(?:Microsoft'
    Replace = '|| false && Regex.IsMatch(text, @"\[\s*(?:Microsoft'
    Tests = 'MigrationTests.Format_AMigrationsDesigner'
}
