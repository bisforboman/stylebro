# A naming rule with a style StyleBro does not write leaves static fields (sourcegit s_)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (IsLeftByNamingRule(field, style) ||'
    Replace = 'if (false ||'
    Tests = 'FieldNamingTests'
}
