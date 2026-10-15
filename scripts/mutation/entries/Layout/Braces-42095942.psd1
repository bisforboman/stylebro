# Trial 22: the braces fix adds BRO1504's blank line below the new }.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'belowCloseBrace: true) => eol'
    Replace = 'belowCloseBrace: false) => eol'
    Tests = 'FixOrderTests.Braces_AndTripleSlash'
}
