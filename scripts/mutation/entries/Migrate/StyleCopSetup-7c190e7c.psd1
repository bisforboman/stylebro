# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '|None|Content|Compile|AdditionalFiles|EmbeddedResource|Folder|Page|Resource)\s)'
    Replace = ')\s)'
    Tests = 'MigrationTests.AnEditorConfigShownInTheSolution_IsntCopied'
}
