# A finding that writes a line break indents the next line.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'WritesLineBreak.IsMatch(line) ? number + 1 : number'
    Replace = 'number'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
