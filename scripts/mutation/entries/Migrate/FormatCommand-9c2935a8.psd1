# Trial 24 (spectre-console): a folder's .editorconfig enforces IDE0005
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Prepend('
    Replace = 'Enumerable.Empty<string>().Prepend('
    Tests = 'MigrationTests.Format_FixesUnusedUsings_WhereOnlyAFolderEnforcesThem'
}
