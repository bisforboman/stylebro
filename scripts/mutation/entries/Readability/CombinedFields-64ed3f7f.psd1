# Fix order: BRO1114's blank line above a documented field's copies (BRO1513)
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'
    Find = ': (docText.Length > 0'
    Replace = ': (false'
    Tests = 'CombinedFieldsTests'
}
