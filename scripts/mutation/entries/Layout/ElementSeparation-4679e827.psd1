# Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '|| IsMultiLineProperty(next, text, autoAccessorLines)'
    Replace = ''
    Tests = 'ElementSeparationTests'
}
