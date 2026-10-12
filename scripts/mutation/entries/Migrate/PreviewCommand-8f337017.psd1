# Trial 9, tool (2026-10-12): edited files keep their line endings
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'File.WriteAllText(file, KeepLineEndings(text, edit(text))'
    Replace = 'File.WriteAllText(file, edit(text)'
    Tests = 'MigrationTests.EditedFiles_KeepTheirLineEndings'
}
