# Trial 25: the copy goes as deep as the files from outside go up
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'parents.Count < up && folder is not null'
    Replace = 'folder is not null'
    Tests = 'MigrationTests.Preview_CopiesTheFilesProjectsCompileFromOutside_ToTheSameRelativePlace'
}
