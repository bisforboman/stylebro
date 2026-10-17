# A T4 template output is listed by itself (trial 28)
@{
    File = 'src/StyleBro.Analyzers/GeneratedFileSuppressor.cs'
    Find = '|| roots.Contains(PackageFileSuppressor.Normalize(path), StringComparer.OrdinalIgnoreCase)'
    Replace = ''
    Tests = 'GeneratedFileSuppressorTests.IsGeneratedFile'
}
