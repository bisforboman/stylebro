# Trial 24: the csharpier tool of a manifest
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'Where(t => t.Name.Equals("csharpier"'
    Replace = 'Where(t => !t.Name.Equals("csharpier"'
    Tests = 'MigrationTests.CSharpier_RunsTheRepositorysTool'
}
