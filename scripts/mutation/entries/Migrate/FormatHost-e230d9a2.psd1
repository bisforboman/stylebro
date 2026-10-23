# trial 49: the load context asks FromFramework first (SDK 8 on .NET 10)
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = 'return FromFramework(assemblyName.Name!, folder, FrameworkFolder) ? null'
    Replace = 'return false ? null'
    Tests = 'FormatHostTests.TheSdkContext_LoadsTheFrameworksCopy_OfAnOlderFrameworkAssembly'
}
