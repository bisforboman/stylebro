# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'outer.Else is not null'
    Replace = 'false'
    Tests = 'NestedIfTests.Skipped'
}
