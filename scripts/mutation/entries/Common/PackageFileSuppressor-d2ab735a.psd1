# a file's path is compared with its '..' segments resolved (trial 19: a Compile item '..\..\ext\lib\*.cs')
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = 'if (part == ".." && parts.Count > 1'
    Replace = 'if (false && part == ".." && parts.Count > 1'
    Tests = 'PackageFileSuppressorTests'
}
