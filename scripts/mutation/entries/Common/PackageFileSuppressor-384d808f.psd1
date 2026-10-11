# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'if (isPackageFile)'
    Replace = 'if (true)'
    Tests = 'PackageFileSuppressorTests'
}
