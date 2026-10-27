# The suppressors asked for run with every analysis (fixture matrix, solution-in-subfolder: the baseline).
@{
    File = 'src/StyleBro.FormatHost/AnalysisCache.cs'
    Find = '                missing = [.. missing.Union(analyzers.OfType<DiagnosticSuppressor>())];'
    Replace = '                missing = [.. missing];'
    Tests = 'StyleBro.Tests.FormatHostFixTests.TheCache_AppliesTheSuppressorsAskedFor'
}
