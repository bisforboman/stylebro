# BRO1149: dynamic conditions are merged unless stylebro_allow_nested_dynamic_ifs is true.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'out var allowed) && allowed
                && rendered.Merged.Any('
    Replace = 'out var allowed) && true
                && rendered.Merged.Any('
    Tests = 'NestedIfTests'
}
