# BRO1509: csharp_indent_case_contents
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'IsFalse(options, "csharp_indent_case_contents") ? string.Empty : unit'
    Replace = 'unit'
    Tests = 'FixOrderTests.Switch_InAnExpandedBody'
}
