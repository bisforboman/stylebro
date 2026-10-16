# The range above a protected line skips the #if blocks some parses leave out.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'open += directive.StartsWith("endif", StringComparison.Ordinal) ? 1 : directive.StartsWith("if", StringComparison.Ordinal) ? -1 : 0;'
    Replace = 'open += 0;'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
