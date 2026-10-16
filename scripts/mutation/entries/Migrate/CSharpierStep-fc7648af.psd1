# Trial 24: only the folders CSharpier formats
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'relative.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'MigrationTests.CSharpier_OnlyTheCSharpFilesItFormats'
}
