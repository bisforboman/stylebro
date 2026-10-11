# stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (options.TryGetValue(StaticStyleKey, out var value))'
    Replace = 'if (false && options.TryGetValue(StaticStyleKey, out var value))'
    Tests = 'FieldNamingTests'
}
