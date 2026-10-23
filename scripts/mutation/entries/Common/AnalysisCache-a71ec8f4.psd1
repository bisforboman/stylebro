# Format host: cached results only for the content they were computed on
@{
    File = 'src/StyleBro.FormatHost/AnalysisCache.cs'
    Find = 'cached.Key != key'
    Replace = 'false'
    Tests = 'FormatHostFixTests.TheCache_RunsAnAnalyzer_OnlyOnContentItHasntSeen'
}
