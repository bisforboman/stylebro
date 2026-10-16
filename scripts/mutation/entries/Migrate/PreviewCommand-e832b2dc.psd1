# No empty MSBuild file is written
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'string.IsNullOrWhiteSpace(edited) && StyleCopSetup.IsMSBuild(file)'
    Replace = 'false'
    Tests = 'MigrationTests.EditFile_NeverWritesAnEmptyProjectFile'
}
