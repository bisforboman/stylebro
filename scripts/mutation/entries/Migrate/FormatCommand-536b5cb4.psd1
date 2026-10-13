# Trial 15: a missing file isn't the same file.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'File.Exists(path) && File.ReadAllText(path).Replace('
    Replace = 'File.ReadAllText(path).Replace('
    Tests = 'FullyQualifiedName~MigrationTests.Verify_ALineEndingOnlyChange_IsNoChange'
}
