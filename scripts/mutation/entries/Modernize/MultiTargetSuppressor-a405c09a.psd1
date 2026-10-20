# The multi-target C# version guard (BRO1145, IDE0049; moved from EmptyRecordBodies in trial 35)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = ' && !string.Equals(langVersion!.Trim(), defaultVersion?.Trim(), StringComparison.OrdinalIgnoreCase)'
    Replace = ''
    Tests = 'EmptyTypeBodyTests|MultiTargetSuppressorTests'
}
