# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'LoadMapping().Where(m => m.Rule != "-").ToList();'
    Replace = 'LoadMapping();'
    Tests = 'MigrationTests.Sonar_'
}
