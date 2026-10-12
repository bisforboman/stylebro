# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (evidence.Count > 0 && !StyleCopSetup.IsInstalled(root))'
    Replace = 'if (evidence.Count < 0)'
    Tests = 'MigrationTests.StyleCopSettingsWithoutThePackage_'
}
