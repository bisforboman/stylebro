# Blank lines above the #if are part of the range a line-break finding may be in.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (open == 0 && text.Length > 0)'
    Replace = 'if (open == 0)'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
