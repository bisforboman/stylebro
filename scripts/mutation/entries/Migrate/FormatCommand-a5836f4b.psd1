# baseline: read the child's output even without a callback
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var all = stream.ReadToEnd();'
    Replace = 'var all = text is null ? string.Empty : stream.ReadToEnd();'
    Tests = 'BaselineTests.ReadOnThread_ReadsTheStreamWithoutACallback'
}
