# Trial 22 (spot a4): only usings the enforced ordering rule would report stay unsorted
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.IsOn("SA1210") && string.Compare('
    Replace = 'if (false && string.Compare('
    Tests = 'MigrationTests.Migrate_SortsUsingsTheEnforcedRuleAcceptsSorted'
}
