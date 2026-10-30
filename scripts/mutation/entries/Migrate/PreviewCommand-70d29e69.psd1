# Trial 62: the text is read in the encoding its byte order mark names.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();'
    Replace = 'new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false);
            text = reader.ReadToEnd();'
    Tests = 'StyleBro.Tests.MigrationTests.EditFile_KeepsAUtf16Or32FilesEncodingAndLineEndings'
}
