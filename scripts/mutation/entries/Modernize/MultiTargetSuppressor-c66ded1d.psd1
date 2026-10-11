# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'if (frameworks.Count < 2)'
    Replace = 'if (frameworks.Count < 1)'
    Tests = 'MultiTargetSuppressorTests'
}
