# A file with '#region' lines is a candidate (Newtonsoft.Json's DynamicTests.cs had no other reason).
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = ' || RegionDirective.IsMatch(text)'
    Replace = ''
    Tests = 'MigrationTests.IfBodies_AlsoAreTheRegionLinesOnlySomeBuildsCompile'
}
