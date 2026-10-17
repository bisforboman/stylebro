# BRO1303/BRO1306: a 'new' field that hides a base member keeps its name
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '&& !PascalCaseNamingAnalyzer.HidesBaseMember(field)'
    Replace = '&& true'
    Tests = 'FieldNamingTests.NewFieldsHidingABaseMember_AreNotReported'
}
