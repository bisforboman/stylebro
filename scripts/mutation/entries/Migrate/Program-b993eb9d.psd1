# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (setup.NotInstalled && sonar is null)'
    Replace = 'if (setup.NotInstalled && sonar is not null)'
    Tests = 'MigrationTests.StyleCopSettingsWithoutThePackage_'
}
