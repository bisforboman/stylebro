# Format host: a copy's run reads the copy's files
@{
    File = 'src/StyleBro.FormatHost/DiskState.cs'
    Find = 'var path = this.onDisk(group.Key);'
    Replace = 'var path = group.Key;'
    Tests = 'FormatHostFixTests.ACopysRuns_ReadAndWriteTheCopysFiles'
}
