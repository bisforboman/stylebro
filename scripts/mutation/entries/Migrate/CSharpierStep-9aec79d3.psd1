# Trial 25: a folder only where CSharpier would change nothing else
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = '                if (Clean(folder))'
    Replace = '                if (Clean(folder) || true)'
    Tests = 'MigrationTests.CSharpier_FormatsWholeFolders_WhereItWouldChangeNothingElse'
}
