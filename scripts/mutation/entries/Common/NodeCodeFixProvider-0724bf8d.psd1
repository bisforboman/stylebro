# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.CodeFixes/NodeCodeFixProvider.cs'
    Find = '.ConfigureAwait(false)).Count == 0)'
    Replace = '.ConfigureAwait(false)).Count < 0)'
    Tests = 'SonarIdsTests.S1066_NoActionWhereBro1149Skips'
}
