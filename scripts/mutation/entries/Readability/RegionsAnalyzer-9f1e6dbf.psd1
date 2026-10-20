# Trial 37: MarkdownSnippets regions stay
@{
    File = 'src/StyleBro.Analyzers/Readability/RegionsAnalyzer.cs'
    Find = 'if (snippets.Contains(Regions.GetName(region)))'
    Replace = 'if (false)'
    Tests = 'SnippetRegions_Stay'
}
