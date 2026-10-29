# Trial 62: a file counts for the form most of its lines have.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '            if (tabs != spaces)
            {
                Add(IndentStyleKey, tabs > spaces);'
    Replace = '            if (tabs != spaces)
            {
                Add(IndentStyleKey, tabs >= 0);'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountFilesIndentedWithTabsOrSpaces'
}
