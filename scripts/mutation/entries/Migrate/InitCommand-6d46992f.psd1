# Trial 12 (2026-10-12): init follows insert_final_newline, column alignment, BRO1527, BRO1113, one-line jumps, mixed code under an unenforced key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = ' && !template.Trim().Equals("unset", StringComparison.OrdinalIgnoreCase)'
    Replace = ''
    Tests = 'MigrationTests.Init_FileHeaderLine_OnlyWithATemplate'
}
