# Trial 42: the builds check held-back ids only where every project enforces code style
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '|| !built.All(p => Capture('
    Replace = '|| false && !built.All(p => Capture('
    Tests = 'MigrationTests.BuildEnforces_'
}
