# Trial 47: deleting a report folder that was never made says nothing.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (!Directory.Exists(folder))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Delete_AFolderThatWasNeverMade_SaysNothing'
}
