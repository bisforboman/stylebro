# BRO1504 (trial 32, Pinta): a comment aligned with a trailing one is compared in columns as shown, tabs counted
@{
    File = 'src/StyleBro.Analyzers/Indentation.cs'
    Find = 'text[i] == ''\t'' ? ((column / tabWidth) + 1) * tabWidth : column + 1'
    Replace = 'column + 1'
    Tests = 'BlankLineAfterTests.CommentAlignedWithTabs'
}
