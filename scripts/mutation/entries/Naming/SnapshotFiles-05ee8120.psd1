# Trial 23: bin/obj copies are skipped
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'if (!SkippedFolders.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase))'
    Replace = 'if (true)'
    Tests = 'KeptFindingsTests.ASnapshotFileOnDisk_IsReadFromTheProjectsRepository'
}
