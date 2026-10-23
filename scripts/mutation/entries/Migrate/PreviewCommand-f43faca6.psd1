# Trial 50: the preview formats its copy with --engine
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'using var engine = engineNamed ? FormatCommand.UseEngine(engineName) : null;'
    Replace = 'using var engine = (IDisposable?)null;'
    Tests = 'MigrationTests.Preview_Format_TakesTheEngineOption'
}
