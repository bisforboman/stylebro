# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (InitCommand.OptsOut(folderSettings))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Migrate_GivesAnOptOutFolder_'
}
