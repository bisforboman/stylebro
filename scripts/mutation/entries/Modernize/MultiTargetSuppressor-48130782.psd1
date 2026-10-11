# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'out var core) && core >= minimum.Core'
    Replace = 'out var core)'
    Tests = 'MultiTargetSuppressorTests'
}
