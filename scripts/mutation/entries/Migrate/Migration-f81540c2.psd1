# migrate (trial 32, ControlzEx): case blocks at the label's level are kept
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'c.Sum() >= Conventions.MinimumAgreeing && Conventions.Winner(c[0], c[1]) == 1'
    Replace = 'false'
    Tests = 'MigrationTests.Migrate_CaseBlocksAtTheLabelsLevel'
}
