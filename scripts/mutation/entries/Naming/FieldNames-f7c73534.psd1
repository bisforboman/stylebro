# Private static fields that aren't readonly get their own style (SX1309S, 2026-10-10)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'm => m.Length == 1 && m[0] == "static"'
    Replace = 'm => false'
    Tests = 'FieldNamingTests'
}
