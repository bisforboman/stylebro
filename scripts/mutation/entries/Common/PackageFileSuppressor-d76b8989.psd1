# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = '? roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))'
    Replace = '? true'
    Tests = 'PackageFileSuppressorTests'
}
