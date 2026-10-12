# Sonar mapping: a narrowing stylebro_ setting isn't written when the StyleCop setup turns the rule on.
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'var onWithoutSonar = !optionOnly && lines.Any('
    Replace = 'var onWithoutSonar = false && lines.Any('
    Tests = 'MigrationTests'
}
