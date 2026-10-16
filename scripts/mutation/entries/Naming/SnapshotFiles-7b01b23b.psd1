# Trial 26: XAML files on disk
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'else if (IsXamlFile(file) && new FileInfo(file).Length < 1 << 20)'
    Replace = 'else if (false)'
    Tests = 'KeptFindingsTests.AXamlFileOnDisk_IsReadFromTheProjectsRepository'
}
