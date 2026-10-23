# Trial 50: the preview formats its copy with --engine
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'using var engine = FormatCommand.UseEngine(engineName);'
    Replace = 'using var engine = FormatCommand.UseEngine(FormatCommand.DotnetFormatEngine);'
    Tests = 'MigrationTests.Preview_Format_TakesTheEngineOption'
}
