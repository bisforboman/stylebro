# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (children.Count == 0)'
    Replace = 'if (true)'
    Tests = 'NestedIfTests'
}
