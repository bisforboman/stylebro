# Trial 22: the braces fix adds BRO1504's blank line below the new }.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'belowCloseBrace: true, tabWidth: Indentation.GetTabWidth(options)) => eol'
    Replace = 'belowCloseBrace: false, tabWidth: Indentation.GetTabWidth(options)) => eol'
    Tests = 'FixOrderTests.Braces_AndTripleSlash'
}
