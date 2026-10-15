# init --diff refuses a StyleCop repository before the preview
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (command == "init" && Path.GetFullPath('
    Replace = 'if (command == "none" && Path.GetFullPath('
    Tests = 'MigrationTests.InitDiff_OnAStyleCopRepository_RefusesBeforeThePreview'
}
