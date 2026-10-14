# BRO1601: the blank lines BRO1506 reports below a comment go with the insertion (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'if (noBlankLineBelowComment && indentation.Trim().Length == 0'
    Replace = 'if (false && indentation.Trim().Length == 0'
    Tests = 'DocumentationTests.InheritDoc_BelowACommentAndABlankLine'
}
