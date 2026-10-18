# preview --help
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '["preview", "--help" or "-h" or "-?" or "help", ..] => ["--help"],'
    Replace = ''
    Tests = 'MigrationTests.TheShortCommands_StandForTheirOptions'
}
