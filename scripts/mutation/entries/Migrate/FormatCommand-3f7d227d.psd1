# A build that changed the root .editorconfig copied it (trial 46)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' || File.ReadAllText(path) != before)'
    Replace = ')'
    Tests = 'MigrationTests.CopyOffVerdict'
}
