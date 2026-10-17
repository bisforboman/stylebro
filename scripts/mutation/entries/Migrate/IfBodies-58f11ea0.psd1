# A finding that writes a line break indents the next line.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '? file.Lines.Contains(number + 1)'
    Replace = '? file.Lines.Contains(number)'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
