# Trial 9: bulk severities only apply to rules on by default
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '|| (defaultSeverity > Severity.None && (byCategory'
    Replace = '|| (true && (byCategory'
    Tests = 'MigrationTests.CategorySeverity_'
}
