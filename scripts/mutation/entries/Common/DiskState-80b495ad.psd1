# Format host: a copy's fix writes the copy's file, not the repository's
@{
    File = 'src/StyleBro.FormatHost/DiskState.cs'
    Find = 'var path = this.onDisk(documentPath);'
    Replace = 'var path = documentPath;'
    Tests = 'FormatHostFixTests.ACopysRuns_ReadAndWriteTheCopysFiles'
}
