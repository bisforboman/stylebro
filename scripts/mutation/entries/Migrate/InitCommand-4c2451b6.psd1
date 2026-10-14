# IDE0009 shown through a category or the bulk severity requires 'this.' (trial 20, MoreLINQ)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var shown = effective is'
    Replace = 'var shown = false && effective is'
    Tests = 'MigrationTests.RequiresThisQualification_ReadsCategoryAndBulkSeverities'
}
