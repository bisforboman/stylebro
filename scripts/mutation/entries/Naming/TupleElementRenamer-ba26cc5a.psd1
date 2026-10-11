# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '|| !TupleElementNames.IsInPublicSignature(element, model, cancellationToken)))'
    Replace = '|| true))'
    Tests = 'TupleElementNamingTests.PublicSignatures_AreLeftAloneByDefault'
}
