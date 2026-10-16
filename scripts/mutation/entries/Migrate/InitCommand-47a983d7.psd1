# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Concat(styleKeys ?? [])'
    Replace = ''
    Tests = 'MigrationTests.ModernizeWithTheStyle_LeavesTheStylesRulesToIt'
}
