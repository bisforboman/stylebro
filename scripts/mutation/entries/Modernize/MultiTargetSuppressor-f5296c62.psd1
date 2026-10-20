# The multi-target C# version guard (BRO1145, IDE0049; moved from EmptyRecordBodies in trial 35)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '                && !AllFrameworksDefaultTo(options, core))'
    Replace = '                )'
    Tests = 'EmptyTypeBodyTests|MultiTargetSuppressorTests'
}
