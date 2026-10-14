# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'return frameworks.Count < 2 ||'
    Replace = 'return frameworks.Count < 1 ||'
    Tests = 'MultiTargetSuppressorTests'
}
