# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'detected.AddRange(teams ? [] : namespaceLines);'
    Replace = 'detected.AddRange(namespaceLines);'
    Tests = 'MigrationTests.Init_NamespaceBraces_EndToEnd'
}
