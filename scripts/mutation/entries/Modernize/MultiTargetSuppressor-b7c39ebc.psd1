# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '&& net.Major >= 5 && net >= minimum.Core'
    Replace = '&& net >= minimum.Core'
    Tests = 'MultiTargetSuppressorTests'
}
