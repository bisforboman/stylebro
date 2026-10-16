# Trial 25: only C# files count in CSharpier's check
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'return code == 0 || (failed.Count == 0 && all.Count > 0) ? null : failed;'
    Replace = 'return code == 0 ? null : failed;'
    Tests = 'MigrationTests.CSharpier_FormatsWholeFolders_WhereItWouldChangeNothingElse'
}
