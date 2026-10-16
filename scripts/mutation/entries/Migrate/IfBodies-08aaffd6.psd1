# A line whose nesting differs between two parses is protected.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'else if (other != depth)'
    Replace = 'else if (other == depth)'
    Tests = 'MigrationTests.IfBodies_AreTheLinesWhoseBodyIfDecides'
}
