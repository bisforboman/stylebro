# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'
    Find = 'start, closeText.Length > 0, text'
    Replace = 'start, false, text'
    Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAfterBro1110'
}
