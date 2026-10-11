# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = 'if (node.Sections.Count < 2 || node.ContainsDiagnostics)'
    Replace = 'if (node.Sections.Count < 2)'
    Tests = 'SwitchSectionBlankLinesTests'
}
