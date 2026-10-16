# Stacked usings are not indented: not a body.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '&& !(node is UsingStatementSyntax && parent is UsingStatementSyntax)'
    Replace = ''
    Tests = 'MigrationTests.IfBodies_AreTheLinesWhoseBodyIfDecides'
}
