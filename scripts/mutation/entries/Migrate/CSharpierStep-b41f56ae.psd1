# verify counts the MSBuild files init edits.
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = '|| IsInitProps(f)).Select'
    Replace = ').Select'
    Tests = 'MigrationTests.CSharpier_TakesTheFilesWhoseLineEndingsArentEndOfLines'
}
