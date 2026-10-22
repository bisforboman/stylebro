# Trial 42: the builds check held-back ids only where every file has them at warning or error
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '.All(f => ids.All(id => Severity('
    Replace = '.All(f => ids.Any(id => true || Severity('
    Tests = 'MigrationTests.BuildEnforces_'
}
