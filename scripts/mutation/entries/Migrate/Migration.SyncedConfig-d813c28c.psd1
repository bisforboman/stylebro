# Trial 40: a project at the root sees only the root's .editorconfig
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'projects.Count == 0 || projects.Any(p => !p.Contains(''/''))'
    Replace = 'projects.Count == 0'
    Tests = 'StyleBro.Tests.MigrationTests.Synced_NeedsTheSubmodulesText_AndEveryProjectInAFolder'
}
