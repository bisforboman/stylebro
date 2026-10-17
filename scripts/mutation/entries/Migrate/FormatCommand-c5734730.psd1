# Trial 28: Compile items picked in a Directory.Build.props
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'files.Add(Path.Combine(folder, "Directory.Build.props"));'
    Replace = '_ = folder;'
    Tests = 'MigrationTests.Format_RunsTheConfigurationsWhoseCompileItemsDiffer'
}
