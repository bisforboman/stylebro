# --json before a short command
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (Expand(args.Where(a => a != JsonReport.Option).ToArray()) is not { } expanded)'
    Replace = 'if (Expand(args) is not { } expanded)'
    Tests = 'MigrationTests.JsonBeforeAShortCommand_RunsThatCommand'
}
