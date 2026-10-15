# Trial 23: snapshots are read from the git root (tests beside the library)
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'var root = GitRoot(folder!) ?? folder!;'
    Replace = 'var root = folder!;'
    Tests = 'KeptFindingsTests.ASnapshotFileOnDisk_IsReadFromTheProjectsRepository'
}
