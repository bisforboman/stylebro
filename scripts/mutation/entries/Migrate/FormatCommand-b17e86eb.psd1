# format holds back the guard's IDE fixes where dotnet format ignores suppressors (trial 35)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var ids = options.Where(o => o == StyleBro.Analyzers.Modernize.MultiTargetSuppressor.TypeNamesId'
    Replace = 'var ids = options.Where(o => o == "none"'
    Tests = 'MigrationTests.HeldBackIds'
}
