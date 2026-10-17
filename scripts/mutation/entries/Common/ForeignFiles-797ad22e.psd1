# Files outside the repository are not analyzed (trial 27)
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = '|| OutsideRepositorySuppressor.IsOutside(path, repository)'
    Replace = '|| false'
    Tests = 'GeneratedFileSuppressorTests.EveryAnalyzer_IsQuietOutsideTheRepository'
}
