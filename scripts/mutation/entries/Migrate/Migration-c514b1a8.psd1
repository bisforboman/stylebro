# format: a template writing another kind of file writes no C#
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(f))'
    Replace = '.Where(File.Exists)'
    Tests = 'MigrationTests.TextTemplateOutputs'
}
