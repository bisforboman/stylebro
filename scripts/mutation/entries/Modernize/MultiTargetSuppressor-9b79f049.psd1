# Multi-target guard (MultiTargetSuppressor)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'minimum.Standard is { } standard && Version.TryParse(name.Substring(11), out var version) && version >= standard'
    Replace = 'minimum.Standard is { } standard'
    Tests = 'MultiTargetSuppressorTests'
}
