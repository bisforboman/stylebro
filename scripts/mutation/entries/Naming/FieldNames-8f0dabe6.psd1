# BRO1307: s_ is the prefix the s_camelCase style asks for, not a prefix to remove
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'var keepsPrefix = casing == FieldCasing.StaticCamel && name.StartsWith("s_", System.StringComparison.Ordinal);'
    Replace = 'var keepsPrefix = false;'
    Tests = 'FieldNamingTests.StaticKeys_SCamelCase_PrefixStaticFields'
}
