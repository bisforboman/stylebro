# Format host: an analyzer runs again only on a compilation it has not seen
@{
    File = 'src/StyleBro.FormatHost/AnalysisCache.cs'
    Find = 'analyzers.Where(a => !cached.ByAnalyzer.ContainsKey(a))'
    Replace = 'analyzers.Where(a => true)'
    Tests = 'FormatHostFixTests.TheCache_RunsAnAnalyzer_OnlyOnACompilationItHasntSeen'
}
