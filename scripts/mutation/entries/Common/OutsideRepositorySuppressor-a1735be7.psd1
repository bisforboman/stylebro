# Files outside the repository: nothing without the property
@{
    File = 'src/StyleBro.Analyzers/OutsideRepositorySuppressor.cs'
    Find = 'return roots.Count == 1 && '
    Replace = 'return '
    Tests = 'PackageFileSuppressorTests.IsOutsideRepository'
}
