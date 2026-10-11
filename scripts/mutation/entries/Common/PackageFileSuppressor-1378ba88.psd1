# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'return roots.Count > 0'
    Replace = 'return false'
    Tests = 'PackageFileSuppressorTests'
}
