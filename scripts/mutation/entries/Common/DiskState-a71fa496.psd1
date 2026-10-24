# Format host: a fix reaches every framework's copy of the file
@{
    File = 'src/StyleBro.FormatHost/DiskState.cs'
    Find = 'foreach (var copy in current.GetDocumentIdsWithFilePath(documentPath))'
    Replace = 'foreach (var copy in current.GetDocumentIdsWithFilePath(documentPath).Take(1))'
    Tests = 'FormatHostFixTests.AFixRun_WritesItsFiles_AsTheWorkspaceSavesThem'
}
