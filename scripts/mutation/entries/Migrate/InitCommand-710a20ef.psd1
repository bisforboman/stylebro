# Trial 12 (2026-10-12): init follows insert_final_newline, column alignment, BRO1527, BRO1113, one-line jumps, mixed code under an unenforced key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var skipRoot = existing is null && '
    Replace = 'var skipRoot = false && '
    Tests = 'MigrationTests.Init_WithAllCodeUnderARootFolder_WritesNoRootEditorConfig'
}
