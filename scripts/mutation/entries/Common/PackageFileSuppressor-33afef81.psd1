# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'path.Replace(''\\'', ''/'').IndexOf("/contentFiles/", StringComparison.OrdinalIgnoreCase) >= 0'
    Replace = 'false'
    Tests = 'PackageFileSuppressorTests'
}
