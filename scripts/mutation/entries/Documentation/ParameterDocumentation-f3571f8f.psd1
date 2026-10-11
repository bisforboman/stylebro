# BRO1611/BRO1613/BRO1614: a top-level <inheritdoc> turns off tag matching, like StyleCop (2026-10-11)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'
    Find = '|| (inherits && kind == TagKind.TypeParameter)'
    Replace = ''
    Tests = 'DocumentationTests.InheritDoc_TurnsOffTagMatching_ButNotParameterNames'
}
