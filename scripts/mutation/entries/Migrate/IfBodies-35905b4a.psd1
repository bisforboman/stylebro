# A check is clean only when the findings it had were all ignored ones.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '&& left.Count < lines.Count &&'
    Replace = '&&'
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
