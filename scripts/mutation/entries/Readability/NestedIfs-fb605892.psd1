# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (node.DescendantTokens().Any(t => text.Lines'
    Replace = 'if (false && node.DescendantTokens().Any(t => text.Lines'
    Tests = 'NestedIfTests.Skipped'
}
