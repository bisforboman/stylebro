# Sonar rule defaults by package version (BRO1150)
@{
    File = 'src/StyleBro.Analyzers/SonarRules.cs'
    Find = '&& GetMajorVersion(paths) is < 10;'
    Replace = ';'
    Tests = 'WhereBeforeTerminalTests.SonarCollectionRules'
}
