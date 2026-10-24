# Trial 50: a framework view reads its additional files
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Item("AdditionalFiles"));'
    Replace = 'null);'
    Tests = 'MigrationTests.AFrameworksView_HasItsAdditionalFiles'
}
