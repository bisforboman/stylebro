# Generator output isn't analyzed.
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = '            || GeneratedFileSuppressor.IsGeneratedFile(path, generated);'
    Replace = ';'
    Tests = 'StyleBro.Tests.GeneratedFileSuppressorTests.EveryAnalyzer_IsQuietInGeneratedFiles'
}
