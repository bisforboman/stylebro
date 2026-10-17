# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '@"^\s*(?:Copyright|COPYRIGHT)\b(.*)$", RegexOptions.Multiline))'
    Replace = '@"^\s*(?:Copyright|COPYRIGHT)\b(.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase))'
    Tests = 'MigrationTests.CopyrightHolder_IgnoresTemplatePlaceholdersAndProse'
}
