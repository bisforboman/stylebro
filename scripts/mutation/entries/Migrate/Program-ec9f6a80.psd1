# Trial 22: init/--write from a subfolder work on the repository root when the build reaches past the folder
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '!(name == ".editorconfig" && editorConfigStops)'
    Replace = 'true'
    Tests = 'MigrationTests.RepositoryFolder_StaysForASelfContainedFolder'
}
