# Format host: a file changed on disk reaches every copy of it
@{
    File = 'src/StyleBro.FormatHost/DiskState.cs'
    Find = 'foreach (var document in group)'
    Replace = 'foreach (var document in group.Take(1))'
    Tests = 'FormatHostFixTests.TheDisk_IsReadBack_IntoEveryCopyOfAChangedFile'
}
