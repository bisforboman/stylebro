# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'Descriptors.TryGetValue(diagnostic.Id, out var descriptor) && '
    Replace = 'Descriptors.TryGetValue("CA1510", out var descriptor) && '
    Tests = 'MultiTargetSuppressorTests'
}
