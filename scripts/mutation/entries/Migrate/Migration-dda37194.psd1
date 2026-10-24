# Trial 52: the contradiction note covers files outvoting the key
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'gitEndings is null && contradicted is null ? Conventions.ConvertedMinorityNote'
    Replace = 'gitEndings is null ? Conventions.ConvertedMinorityNote'
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
