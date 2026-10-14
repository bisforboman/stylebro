# BRO1601: only blank lines that reach the member
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = '&& blankLines[blankLines.Count - 1].LineNumber == line.LineNumber - 1'
    Replace = ''
    Tests = 'DocumentationTests.InheritDoc_BelowACommentBlankLineAndDirective'
}
