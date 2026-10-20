# format holds back the guard's IDE fixes where dotnet format ignores suppressors (trial 35)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(o.StartsWith("IDE", StringComparison.Ordinal) && StyleBro.Analyzers.Modernize.MultiTargetSuppressor.Minimums.ContainsKey(o))'
    Replace = '(StyleBro.Analyzers.Modernize.MultiTargetSuppressor.Minimums.ContainsKey(o))'
    Tests = 'MigrationTests.HeldBackIds'
}
