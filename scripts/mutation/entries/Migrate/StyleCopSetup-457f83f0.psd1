# A scope folded into the StyleCop base keeps its Sonar severities.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '                if (scope.Sonar is not null)'
    Replace = '                if (false)'
    Tests = 'MigrationTests.Sonar_AScopeOverEveryStyleCopFolder'
}
