# Files outside the repository: only full paths
@{
    File = 'src/StyleBro.Analyzers/OutsideRepositorySuppressor.cs'
    Find = '&& IsFullPath(PackageFileSuppressor.Normalize(path)) '
    Replace = ''
    Tests = 'PackageFileSuppressorTests.IsOutsideRepository'
}
