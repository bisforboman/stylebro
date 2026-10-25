# init's summary counts a rule two conventions turn off once (trial 54)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.GroupBy(e => e.Rule)'
    Replace = '.GroupBy(e => e.Rule + e.What)'
    Tests = 'MigrationTests.Summary_CountsARuleTwoConventionsTurnOffOnce'
}
