# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'outer.ContainsDirectives'
    Replace = 'false'
    Tests = 'NestedIfTests.Skipped'
}
