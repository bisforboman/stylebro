# Only line-break findings above a protected line are ignored.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'return LineBreakOnly.IsMatch(line) && file.Lines'
    Replace = 'return file.Lines'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
