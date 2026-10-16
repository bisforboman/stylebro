# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '(project[lineStart..end].Trim().Length == 0 ? string.Empty : newLine)'
    Replace = 'string.Empty'
    Tests = 'MigrationTests.AddItem_PutsTheGroupOnLinesOfItsOwn'
}
