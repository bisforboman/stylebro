# The multi-target C# version guard (BRO1145, IDE0049; moved from EmptyRecordBodies in trial 35)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '.Any(u => u.Alias is not null)'
    Replace = '.Any(u => false)'
    Tests = 'EmptyTypeBodyTests|MultiTargetSuppressorTests'
}
