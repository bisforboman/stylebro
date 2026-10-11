# Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'if (blankLineBefore && indentation.Trim().Length == 0'
    Replace = 'if (false && indentation.Trim().Length == 0'
    Tests = 'DocumentationTests'
}
