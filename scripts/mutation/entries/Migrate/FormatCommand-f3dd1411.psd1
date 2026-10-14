# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Conventions.CSharpier(root) is null ? string.Empty : " (CSharpier''s code)"'
    Replace = '" (CSharpier''s code)"'
    Tests = 'MigrationTests.Format_NamesNoFormatter_'
}
