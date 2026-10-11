# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'return scope is not null'
    Replace = 'return false && scope is not null'
    Tests = 'NestedIfTests.Skipped'
}
