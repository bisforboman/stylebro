# Trial 22 (kafkaflow): no block in a root = true folder without C# files
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Any(c => c.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))))'
    Replace = '.Any()))'
    Tests = 'MigrationTests.Migrate_SkipsRootTrueFoldersWithoutCSharp'
}
