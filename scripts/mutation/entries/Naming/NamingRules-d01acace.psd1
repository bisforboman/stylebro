# BRO1303: a #pragma warning disable IDE1006 keeps names (MoreLINQ, trial 20)
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '|| IsDisabledByPragma(tree, location.SourceSpan.Start, cancellationToken)'
    Replace = '|| false'
    Tests = 'FieldNamingTests.FieldsWhereIde1006IsDisabled'
}
