# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'frameworks.All(f => Has(f, minimum))'
    Replace = 'frameworks.Any(f => Has(f, minimum))'
    Tests = 'MultiTargetSuppressorTests'
}
