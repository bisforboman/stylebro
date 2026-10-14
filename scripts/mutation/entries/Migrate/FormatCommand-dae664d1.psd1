# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!NotCompiled(f, project) && '
    Replace = ''
    Tests = 'MigrationTests.Format_SaysNothingAboutAGeneratedFile'
}
