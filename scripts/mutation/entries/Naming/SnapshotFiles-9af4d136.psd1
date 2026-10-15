# Trial 23: binary snapshot files are skipped
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = ' && text.IndexOf(''\0'') < 0'
    Replace = ''
    Tests = 'KeptFindingsTests.ASnapshotFileOnDisk_IsReadFromTheProjectsRepository'
}
