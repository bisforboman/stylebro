# Trial 24 (refit): a framework's own Compile items
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'framework is null ? outer : outer.Union'
    Replace = 'true ? outer : outer.Union'
    Tests = 'MigrationTests.Format_FormatsFilesLinkedForSomeFrameworksOnly'
}
