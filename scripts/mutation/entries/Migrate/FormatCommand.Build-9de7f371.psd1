# Trial 58 (LightInject): the HEAD copy keeps format's ids as errors in the folders format leaves alone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'var outside = string.Join(" and ", (leftAlone ?? [])'
    Replace = 'var outside = string.Join(" and ", (Array.Empty<string>())'
    Tests = 'MigrationTests.UntouchedHook_KeepsFormatsRulesAsTheyAre_InTheFoldersFormatLeavesAlone'
}
