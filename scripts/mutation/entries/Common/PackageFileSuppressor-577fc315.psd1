# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'Normalize(f.Trim()).TrimEnd(''/''))'
    Replace = 'Normalize(f.Trim()))'
    Tests = 'PackageFileSuppressorTests'
}
