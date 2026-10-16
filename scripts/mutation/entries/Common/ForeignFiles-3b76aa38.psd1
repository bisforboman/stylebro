# Submodule files aren't analyzed.
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = '            || PackageFileSuppressor.IsSubmoduleFile(path, submodules)'
    Replace = ''
    Tests = 'StyleBro.Tests.GeneratedFileSuppressorTests.EveryAnalyzer_IsQuietInSubmoduleFiles'
}
