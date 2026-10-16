# BRO1149: only lines aligned under the '(' keep their place relative to the condition.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'lines.Min(l => IndentOf(text, l.Start)) == paren'
    Replace = 'lines.Min(l => IndentOf(text, l.Start)) >= 0'
    Tests = 'StyleBro.Tests.NestedIfTests'
}
