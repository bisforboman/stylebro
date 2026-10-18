# Trial 30 (2026-10-18): only the SDK's MissingWorkloadPack items count
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& items.TryGetProperty("MissingWorkloadPack", out var packs)'
    Replace = '&& items.TryGetProperty("Compile", out var packs)'
    Tests = 'MigrationTests.MissingWorkloadPacks_ReadsTheSdksItems'
}
