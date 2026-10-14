# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = 'var keywords = top ? type is not { SpecialType: not SpecialType.None } :'
    Replace = 'var keywords = top ? true :'
    Tests = 'QualifiedUsingsTests'
}
