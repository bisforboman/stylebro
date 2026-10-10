# Mutations for Modernize (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # Multi-target guard (MultiTargetSuppressor)
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'if (frameworks.Count < 2)'; Replace = 'if (frameworks.Count < 1)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = '!frameworks.All(f => Has(f, Minimums[diagnostic.Id]))'; Replace = '!frameworks.Any(f => Has(f, Minimums[diagnostic.Id]))'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'Descriptors.TryGetValue(diagnostic.Id, out var descriptor) && '; Replace = 'Descriptors.TryGetValue("CA1510", out var descriptor) && '; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'var name = framework.ToLowerInvariant();'; Replace = 'var name = framework;'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'if (dash >= 0)'; Replace = 'if (false)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'out var core) && core >= minimum.Core'; Replace = 'out var core)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'minimum.Standard is { } standard && Version.TryParse(name.Substring(11), out var version) && version >= standard'; Replace = 'minimum.Standard is { } standard'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = '&& net.Major >= 5 && net >= minimum.Core'; Replace = '&& net >= minimum.Core'; Tests = 'MultiTargetSuppressorTests' }
    )
}
