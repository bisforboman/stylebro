# Trial 22: init/--write from a subfolder work on the repository root when the build reaches past the folder
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '&& Regex.IsMatch(File.ReadAllText(f), @"""\.\.[\\/]"))'
    Replace = '&& false)'
    Tests = 'MigrationTests.Init_FromTheSolutionsFolder_WritesWhereTheProjectsAre'
}
