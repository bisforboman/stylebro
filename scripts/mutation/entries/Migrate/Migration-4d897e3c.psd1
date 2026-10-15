# Trial 22 (kafkaflow): mixed operator spacing gets 'ignore'
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(winner == 1 || (winner < 0 && key == "csharp_space_around_binary_operators"))'
    Replace = '(winner == 1)'
    Tests = 'MigrationTests.Migrate_SpacingRulesOff_KeepTheCodesSpacing_OrNameWhatFormatChanges'
}
