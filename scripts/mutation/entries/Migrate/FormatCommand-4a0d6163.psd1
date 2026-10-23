# trial 48: check names undone files only at the content format left
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && hash == Hash(File.ReadAllBytes(f))'
    Replace = ''
    Tests = 'FormatCoverageTests.Format_NamesTheFilesEachRunUndoes'
}
