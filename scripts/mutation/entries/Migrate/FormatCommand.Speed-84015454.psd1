# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'if (RestoreStamp(root, projects.Select(p => p.AssetsFile), stampFolder) is (not null, var warning))'
    Replace = 'if (RestoreStamp(root, projects.Select(p => p.AssetsFile), stampFolder) is (not null, var warning) && false)'
    Tests = 'MigrationTests.Format_SkipsTheRestore_AfterItsOwnNoOpRestore'
}
