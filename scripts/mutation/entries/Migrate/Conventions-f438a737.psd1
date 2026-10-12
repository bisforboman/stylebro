# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'QualifiedNameSyntax q when q.Right == identifier => q.Left.ToString() is "System" or "global::System"'
    Replace = 'QualifiedNameSyntax q when q.Right == identifier => true'
    Tests = 'MigrationTests.Conventions_CountTypeAliasFiles'
}
