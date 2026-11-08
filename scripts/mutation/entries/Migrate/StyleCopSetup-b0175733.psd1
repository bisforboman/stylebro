# Issue #339: early 1.3 betas lack some of 1.3s new rules
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'MissingIn13.Where(m => build < m.Since)'
    Replace = 'MissingIn13.Where(m => build < 0)'
    Tests = 'MigrationTests.EarlyStyleCop13Betas_LackSomeOfItsNewRules'
}
