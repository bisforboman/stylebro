# Trial 22: a new Directory.Build.props imports the one above it
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var import = importParent ?'
    Replace = 'var import = false ?'
    Tests = 'MigrationTests.SetPreset_ImportsTheParentProps_InANewFile'
}
