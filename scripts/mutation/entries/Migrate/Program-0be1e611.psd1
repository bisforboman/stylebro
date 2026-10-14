# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '        if (setup.NotInstalled)'
    Replace = '        if (setup.NotInstalled && sonar is null)'
    Tests = 'MigrationTests.Migrate_ALeftoverStyleCopJsonNextToSonar_'
}
