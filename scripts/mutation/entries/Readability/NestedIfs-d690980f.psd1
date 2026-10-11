# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'IndentOf(text, current.SpanStart) - topIndent'
    Replace = '0'
    Tests = 'NestedIfTests.MultiLineInnerCondition_MovesLeft'
}
