# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'if (first == last || item.DescendantTokens()'
    Replace = 'if (first == last || false && item.DescendantTokens()'
    Tests = 'ParameterLayoutTests'
}
