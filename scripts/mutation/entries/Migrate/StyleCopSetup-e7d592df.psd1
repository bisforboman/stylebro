# A path section in braces ({src/tests/**/*.cs,tests/**/*.cs}, tfplan2md) is read.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '|\.cs\s*[,}]")'
    Replace = '")'
    Tests = 'MigrationTests.Sonar_APathSectionLowersTheFixingRule'
}
