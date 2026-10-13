# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'return roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));'
    Replace = 'return true;'
    Tests = 'PackageFileSuppressorTests'
}
