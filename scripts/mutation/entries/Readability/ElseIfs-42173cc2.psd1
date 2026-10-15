@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'return indent - shift > elseIndent ? shift : indent > elseIndent ? 0 : -1;'
    Replace = 'return shift;'
    Tests = 'ElseIfTests.IfLeftUnindentedByTheFormatter'
}
