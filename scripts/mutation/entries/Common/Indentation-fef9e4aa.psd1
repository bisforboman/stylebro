# BRO1504 (trial 32, Pinta): tabs are as wide as tab_width says
@{
    File = 'src/StyleBro.Analyzers/Indentation.cs'
    Find = 'foreach (var key in new[] { "tab_width", "indent_size" })'
    Replace = 'foreach (var key in new string[0])'
    Tests = 'BlankLineAfterTests.CommentAlignedWithTabs'
}
