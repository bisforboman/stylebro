# Trial 15 (Moonglade): name the Directory.Build.props the projects import.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'in {props}.");'
    Replace = 'in Directory.Build.props.");'
    Tests = 'MigrationTests.Init_NamesTheDirectoryBuildPropsTheProjectsImport'
}
