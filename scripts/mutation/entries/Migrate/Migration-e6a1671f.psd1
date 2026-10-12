# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(marked.Count == 0 || !marked.Any('
    Replace = '(marked.Count == 0 || marked.Any('
    Tests = 'MigrationTests.Conventions_SkipFoldersTheRepositoryMarksGenerated'
}
