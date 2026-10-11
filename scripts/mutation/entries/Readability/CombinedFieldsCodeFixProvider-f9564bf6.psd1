# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.CodeFixes/Readability/CombinedFieldsCodeFixProvider.cs'
    Find = 'CombinedFields.GetChange(field, text, allowAdjacent)'
    Replace = 'CombinedFields.GetChange(field, text)'
    Tests = 'CombinedFieldsTests'
}
