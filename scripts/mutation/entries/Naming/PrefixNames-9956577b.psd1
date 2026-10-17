# Trial 27: _TVertex -> TVertex.
@{
    File = 'src/StyleBro.Analyzers/Naming/PrefixNames.cs'
    Find = 'if (core[0] == prefix)'
    Replace = 'if (false)'
    Tests = 'PrefixNamingTests'
}
