# BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '!(Readability.EmbeddedComments.GetMovingRule(comment, text) is { } rule && isOn(rule))'
    Replace = 'true'
    Tests = 'BlankLineAfterTests'
}
