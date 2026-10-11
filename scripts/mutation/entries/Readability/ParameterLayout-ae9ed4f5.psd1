# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'foreach (var change in Reindent(item, text, indentation))'
    Replace = 'foreach (var change in Reindent(item, text, indentation).Take(0))'
    Tests = 'ParameterLayoutTests'
}
