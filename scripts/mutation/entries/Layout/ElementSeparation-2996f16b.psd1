# Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '(PropertyDeclarationSyntax property, PropertyDeclarationSyntax next) => IsMultiLineProperty(property, text, autoAccessorLines) || IsMultiLineProperty(next, text, autoAccessorLines)'
    Replace = '(PropertyDeclarationSyntax property, PropertyDeclarationSyntax next) => true'
    Tests = 'ElementSeparationTests'
}
