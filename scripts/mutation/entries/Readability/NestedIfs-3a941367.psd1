# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| outer.GetLastToken().GetNextToken().LeadingTrivia.Any(t => t.IsDirective)'
    Replace = ''
    Tests = 'NestedIfTests.Skipped'
}
