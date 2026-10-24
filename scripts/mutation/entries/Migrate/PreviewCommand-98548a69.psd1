# Trial 51: MakeRepository marks them -text
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'File.AppendAllLines(Path.Combine(folder, ".git", "info", "attributes"), kept.Select(p => $"/{p} -text"));'
    Replace = ''
    Tests = 'MigrationTests.Preview_TheCopy_KeepsFilesCommittedWithCrLf'
}
