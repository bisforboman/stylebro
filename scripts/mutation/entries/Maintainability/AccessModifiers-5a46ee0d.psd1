# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'
    Find = ' || symbol is INamedTypeSymbol { IsFileLocal: true })'
    Replace = ')'
    Tests = 'AccessModifiersTests'
}
