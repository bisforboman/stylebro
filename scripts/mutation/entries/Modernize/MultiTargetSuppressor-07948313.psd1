# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'if (dash >= 0)'
    Replace = 'if (false)'
    Tests = 'MultiTargetSuppressorTests'
}
