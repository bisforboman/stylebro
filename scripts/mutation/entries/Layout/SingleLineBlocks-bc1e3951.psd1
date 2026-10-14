# BRO1509: csharp_indent_switch_labels
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'IsFalse(options, "csharp_indent_switch_labels") ? string.Empty : unit'
    Replace = 'unit'
    Tests = 'FixOrderTests.Switch_InAnExpandedBody'
}
