# trial 49: the framework's copy of an assembly at the SDK copy's version wins (the .NET host's rule)
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = 'AssemblyName.GetAssemblyName(framework).Version >= AssemblyName.GetAssemblyName(own).Version'
    Replace = 'AssemblyName.GetAssemblyName(framework).Version > AssemblyName.GetAssemblyName(own).Version'
    Tests = 'FormatHostTests.TheSdkContext_TakesTheFrameworksCopy_UnlessTheSdksIsNewer'
}
