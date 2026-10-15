# Trial 22 (spot a4): only usings the enforced ordering rule would report stay unsorted
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (namespaces && systemApart && IsSystem(a) != IsSystem(b))'
    Replace = 'if (namespaces && IsSystem(a) != IsSystem(b))'
    Tests = 'MigrationTests.Migrate_SortsUsingsTheEnforcedRuleAcceptsSorted'
}
