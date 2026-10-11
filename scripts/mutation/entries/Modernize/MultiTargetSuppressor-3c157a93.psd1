# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '!frameworks.All(f => Has(f, Minimums[diagnostic.Id]))'
    Replace = '!frameworks.Any(f => Has(f, Minimums[diagnostic.Id]))'
    Tests = 'MultiTargetSuppressorTests'
}
