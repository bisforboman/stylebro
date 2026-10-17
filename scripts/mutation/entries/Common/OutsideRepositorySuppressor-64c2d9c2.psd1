# Files outside the repository: 'none' turns it off
@{
    File = 'src/StyleBro.Analyzers/OutsideRepositorySuppressor.cs'
    Find = '!roots[0].Equals("none", StringComparison.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'PackageFileSuppressorTests.IsOutsideRepository'
}
