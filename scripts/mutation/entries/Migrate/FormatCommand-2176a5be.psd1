# format: only a template's own output is generated (trial 29, Moq)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& string.Equals(Migration.TextTemplateOutput(Path.Combine(folder, s + ".tt")), file, StringComparison.OrdinalIgnoreCase))'
    Replace = ')'
    Tests = 'MigrationTests.TextTemplateOutputs'
}
