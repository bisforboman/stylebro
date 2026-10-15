# Trial 22 (kafkaflow): suppressions are carried over only where StyleCop ran
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (folders is not null && !folders.Any('
    Replace = 'if (false && !folders!.Any('
    Tests = 'MigrationTests.Migrate_CarriesSuppressionsOnlyWhereStyleCopRan'
}
