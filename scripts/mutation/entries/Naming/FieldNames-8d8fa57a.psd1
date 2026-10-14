# Trial 21: SA1310 skips names starting with an underscore
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'casing != FieldCasing.UnderscoreCamel || '
    Replace = 'true || '
    Tests = 'FieldNamingTests.UnderscoreStyle_LeavesUnderscoresInANameStartingWithOne'
}
