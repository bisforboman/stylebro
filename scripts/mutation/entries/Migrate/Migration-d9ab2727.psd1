# Trial 22 (spot a4): only usings the enforced ordering rule would report stay unsorted
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var held = unsorted.Where(f => !AcceptsSorted('
    Replace = 'var held = unsorted.Where(f => true || !AcceptsSorted('
    Tests = 'MigrationTests.Migrate_SortsUsingsTheEnforcedRuleAcceptsSorted'
}
