# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = 'top && type is { SpecialType: not SpecialType.None } ? QualifiedNoKeywords : Qualified'
    Replace = 'Qualified'
    Tests = 'QualifiedUsingsTests'
}
