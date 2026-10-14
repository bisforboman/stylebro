# Trial 21: migrate writes SA1512's removal of the blank line below standalone comments
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.IsOn("SA1512"))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.StandaloneComments_KeepSA1512sRemoval'
}
