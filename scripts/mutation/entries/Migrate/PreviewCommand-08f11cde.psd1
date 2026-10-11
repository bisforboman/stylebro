# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (NoNewlineAfter(''-'') != NoNewlineAfter(''+''))'
    Replace = 'if (false)'
    Tests = 'MigrationTests'
}
