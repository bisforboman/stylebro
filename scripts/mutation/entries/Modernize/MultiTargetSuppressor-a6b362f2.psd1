# The multi-target C# version guard (BRO1145, IDE0049; moved from EmptyRecordBodies in trial 35)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = 'return name is "IntPtr" or "UIntPtr" ? new Version(5, 0) : null;'
    Replace = 'return name is "UIntPtr" ? new Version(5, 0) : null;'
    Tests = 'EmptyTypeBodyTests|MultiTargetSuppressorTests'
}
