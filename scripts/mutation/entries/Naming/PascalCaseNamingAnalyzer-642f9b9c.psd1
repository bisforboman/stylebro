# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
# Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| ((symbol as INamedTypeSymbol ?? symbol.ContainingType) is { } owner && NamespaceNames.HasGeneratedPart(owner))'
    Replace = ''
    Tests = 'PascalCaseNamingTests'
}
