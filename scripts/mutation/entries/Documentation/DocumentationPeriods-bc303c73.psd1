# Trial 21: version-like endings
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || VersionLike.IsMatch(word)'
    Replace = ''
    Tests = 'DocumentationTests.TextEndingInAVersionNumber_GetsNoPeriod'
}
