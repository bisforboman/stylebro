# Format host: an analyzer runs again only on content it has not seen
@{
    File = 'src/StyleBro.FormatHost/AnalysisCache.cs'
    Find = 'analyzers.Where(a => !cached.ByAnalyzer.ContainsKey(a))'
    Replace = 'analyzers.Where(a => true)'
    Tests = 'FormatHostFixTests.TheCache_RunsAnAnalyzer_OnlyOnContentItHasntSeen'
}
