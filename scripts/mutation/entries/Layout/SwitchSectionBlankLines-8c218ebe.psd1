# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = '|| !next.LeadingTrivia.All('
    Replace = '|| false && !next.LeadingTrivia.All('
    Tests = 'SwitchSectionBlankLinesTests'
}
