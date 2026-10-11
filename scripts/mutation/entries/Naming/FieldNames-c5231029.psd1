# Private static fields that aren't readonly get their own style (SX1309S, 2026-10-10)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'field.IsStatic && style.MutableStatic is { } mutableStatic ? mutableStatic : style.Private'
    Replace = 'style.Private'
    Tests = 'FieldNamingTests'
}
