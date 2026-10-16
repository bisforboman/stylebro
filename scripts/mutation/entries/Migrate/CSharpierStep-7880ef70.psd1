# The rules CSharpier undoes are named.
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'ids is { Count: > 0 }'
    Replace = 'false'
    Tests = 'MigrationTests.CSharpier_PutBack_NamesTheRulesWhoseEditsItUndoes'
}
