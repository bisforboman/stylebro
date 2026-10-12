# BRO1149: dynamic conditions are merged unless stylebro_allow_nested_dynamic_ifs is true.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '(options.TryGetValue(AllowDynamicKey, out var allow) && bool.TryParse(allow.Trim(), out var allowed) && allowed'
    Replace = '(true'
    Tests = 'NestedIfTests'
}
