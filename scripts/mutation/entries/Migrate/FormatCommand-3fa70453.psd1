# Trial 8, tool (2026-10-11): a project importing Sdk.props is SDK-style
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|<Import\b[^>]*\bSdk\s*='
    Replace = ''
    Tests = 'MigrationTests.OldStyleProjects_'
}
