# An else body counts as a body.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '&& (node.Parent is ElseClauseSyntax'
    Replace = '&& (false'
    Tests = 'MigrationTests.IfBodies_AreTheLinesWhoseBodyIfDecides'
}
