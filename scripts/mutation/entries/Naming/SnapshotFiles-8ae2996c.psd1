# Trial 23: huge snapshot files are skipped
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'new FileInfo(file).Length < 1 << 20 && '
    Replace = ''
    Tests = 'KeptFindingsTests.ASnapshotFileOnDisk_IsReadFromTheProjectsRepository'
}
