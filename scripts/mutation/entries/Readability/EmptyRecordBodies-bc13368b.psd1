# BRO1140 (empty record body) and BRO1145 (empty class, struct or interface body)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'
    Find = '.LanguageVersion >= LanguageVersion.CSharp12 ?'
    Replace = '.LanguageVersion >= LanguageVersion.CSharp1 ?'
    Tests = 'EmptyTypeBodyTests'
}
