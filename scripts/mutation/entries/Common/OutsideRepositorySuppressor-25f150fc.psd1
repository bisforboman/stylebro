# Files outside the repository: findings suppressed
@{
    File = 'src/StyleBro.Analyzers/OutsideRepositorySuppressor.cs'
    Find = 'if (isOutside)'
    Replace = 'if (false)'
    Tests = 'PackageFileSuppressorTests.SuppressesFindingsOutsideTheRepository'
}
