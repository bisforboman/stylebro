# Trial 25: a path under another one is given once
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'return chosen.Where(c => !chosen.Any('
    Replace = 'return chosen.Where(c => true || !chosen.Any('
    Tests = 'MigrationTests.CSharpier_FormatsWholeFolders_WhereItWouldChangeNothingElse'
}
