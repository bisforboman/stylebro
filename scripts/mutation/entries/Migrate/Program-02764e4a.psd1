# A command name after migrate is a folder
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '["migrate", "init" or "format" or "baseline" or "help" or "migrate" or "check" or "preview", .. var rest] => ["./" + args[1], .. rest],'
    Replace = ''
    Tests = 'MigrationTests.TheShortCommands_StandForTheirOptions'
}
