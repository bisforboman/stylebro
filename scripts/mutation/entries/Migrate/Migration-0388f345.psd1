# format: a T4 template's output directive names the file it writes (trial 29, Moq)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return directive.Success ? directive.Groups["extension"].Value.Trim() : ".cs";'
    Replace = 'return ".cs";'
    Tests = 'MigrationTests.TextTemplateOutputs'
}
