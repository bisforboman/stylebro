# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'var name = framework.ToLowerInvariant();'
    Replace = 'var name = framework;'
    Tests = 'MultiTargetSuppressorTests'
}
