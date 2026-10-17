# format: a T4 template's output directive names the file it writes (trial 29, Moq)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'directive.Success ? directive.Groups["extension"].Value.Trim() : ".cs");'
    Replace = '".cs");'
    Tests = 'MigrationTests.TextTemplateOutputs'
}
