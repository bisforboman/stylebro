# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '&& setup.ReferencedIn.Count == StyleCopSetup.EnumerateFiles(root).Count(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));'
    Replace = ';'
    Tests = 'MigrationTests.PackageHints_MentionTheRootProps_WhenEveryProjectReferencesStyleCop'
}
