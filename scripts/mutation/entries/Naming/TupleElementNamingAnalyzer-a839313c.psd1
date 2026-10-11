# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '|| (!PublicApi.IsRenameAllowed(options) && TupleElementNames.IsInPublicSignature('
    Replace = '|| (false && TupleElementNames.IsInPublicSignature('
    Tests = 'TupleElementNamingTests.PublicSignatures_AreLeftAloneByDefault'
}
