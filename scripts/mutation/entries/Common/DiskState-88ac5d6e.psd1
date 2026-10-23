# Format host: a file written again with the same text is not a change
@{
    File = 'src/StyleBro.FormatHost/DiskState.cs'
    Find = 'if (text.ContentEquals(before) && Equals(text.Encoding, before.Encoding))'
    Replace = 'if (false)'
    Tests = 'FormatHostFixTests.TheDisk_IsReadBack_IntoEveryCopyOfAChangedFile'
}
