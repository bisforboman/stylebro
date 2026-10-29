# Trial 62: a UTF-16 file must be written back in its own encoding.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (reader.CurrentEncoding is not UTF8Encoding)'
    Replace = 'if (false)'
    Tests = 'StyleBro.Tests.MigrationTests.EditFile_KeepsAUtf16Or32FilesEncodingAndLineEndings'
}
