# Trial 50: every preview takes --engine
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'var previewEngine = command != "baseline" && PreviewCommand.Wants(options) ? FormatCommand.TakeEngine'
    Replace = 'var previewEngine = command == "never" && PreviewCommand.Wants(options) ? FormatCommand.TakeEngine'
    Tests = 'MigrationTests.Preview_Init_TakesTheEngineOption'
}
