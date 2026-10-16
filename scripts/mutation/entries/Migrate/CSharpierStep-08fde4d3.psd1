# Trial 24 (Dawnshard): CSharpier puts back a fix
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'now == was'
    Replace = 'true'
    Tests = 'MigrationTests.CSharpier_OnlyTheCSharpFilesItFormats'
}
