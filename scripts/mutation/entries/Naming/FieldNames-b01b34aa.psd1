# BRO1307/BRO1308: a joined name in the s_camelCase style gets s_
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'result = "s_" + result;'
    Replace = 'result = "_" + result;'
    Tests = 'FieldNamingTests.StaticKeys_SCamelCase_PrefixStaticFields'
}
