# Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = '!IsExplicitImplementation(s) && '
    Replace = ''
    Tests = 'DocumentationTests'
}
