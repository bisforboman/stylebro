# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.Folders is not null && setup.FoldersWithout.Any('
    Replace = 'if (setup.Folders is null && setup.FoldersWithout.Any('
    Tests = 'MigrationTests.ARootTrueFolderWithoutStyleCop_'
}
