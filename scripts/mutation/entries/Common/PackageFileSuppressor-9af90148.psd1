# Trial 18: findings in git submodule code are hidden (Fonts)
@{
    File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'
    Find = ' || IsSubmoduleFile(tree.FilePath, submodules);'
    Replace = ';'
    Tests = 'PackageFileSuppressorTests.SuppressesStyleBroFindingsInSubmoduleFiles'
}
