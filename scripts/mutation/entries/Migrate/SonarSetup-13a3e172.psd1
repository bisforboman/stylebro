# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'applied.NotCovered.AddRange(NotCovered.Where(m => IsOn(m.Sonar)).Select(m => (m.Sonar, m.Note!)));'
    Replace = ''
    Tests = 'MigrationTests.Sonar_S3260'
}
