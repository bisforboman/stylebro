# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.CodeFixes/Readability/ParameterLayoutCodeFixProvider.cs'
    Find = '!remaining.Any(o => o != l && o.Span.Contains(l.Span))'
    Replace = 'true'
    Tests = 'ParameterLayoutTests'
}
