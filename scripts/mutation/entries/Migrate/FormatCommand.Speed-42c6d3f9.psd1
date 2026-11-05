# format keeps the indentation inside #if, and its check does not count those findings (079, Newtonsoft).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'AsKept(l, known) is null && !(keptIndentation is not null && IfBodies.IsKeptFinding(l, keptIndentation))'
    Replace = 'AsKept(l, known) is null'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
