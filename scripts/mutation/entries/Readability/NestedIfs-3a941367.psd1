# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| HasConditionalDirective(outer.GetLastToken().GetNextToken().LeadingTrivia)'
    Replace = ''
    Tests = 'NestedIfTests.Skipped'
}
