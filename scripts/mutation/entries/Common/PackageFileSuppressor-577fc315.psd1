# Files from NuGet packages (PackageFileSuppressor)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = '.Replace(''\\'', ''/'').TrimEnd(''/''))'
    Replace = '.Replace(''\\'', ''/''))'
    Tests = 'PackageFileSuppressorTests'
}
