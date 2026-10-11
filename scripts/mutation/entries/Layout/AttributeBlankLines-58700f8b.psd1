# BRO1525 (no blank line after attributes)
@{
    File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'
    Find = ' || list.Parent.ContainsDiagnostics)'
    Replace = ')'
    Tests = 'AttributeBlankLinesTests'
}
