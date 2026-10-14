# BRO1616 skips a malformed comment
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = '&& !DocumentationComments.IsMalformed(documentation)'
    Replace = ''
    Tests = 'SummaryLayoutTests.SummariesWithoutTheirEndTag_AreNotReported'
}
