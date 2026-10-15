# Trial 22: the joined if's lines go where the chain wants them.
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = '            if (parts.Count == 1)'
    Replace = '            if (false)'
    Tests = 'ElseIfTests.IfLeftUnindentedByTheFormatter'
}
