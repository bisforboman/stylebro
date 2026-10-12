# Sonar mapping: a row's settings are '; '-separated (S121).
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '.SelectMany(s => s.Split("; "))'
    Replace = ''
    Tests = 'MigrationTests'
}
