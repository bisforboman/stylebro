# Package files aren't analyzed.
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = 'return PackageFileSuppressor.IsPackageFile(path, packages)'
    Replace = 'return false'
    Tests = 'StyleBro.Tests.GeneratedFileSuppressorTests.EveryAnalyzer_IsQuietInGeneratedFiles'
}
