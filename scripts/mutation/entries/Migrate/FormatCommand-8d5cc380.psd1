# HasOtherLineEnding: a CR is the other ending on Linux and macOS only.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!onWindows)'
    Replace = 'if (onWindows)'
    Tests = 'FormatLineEndingsTests.HasOtherLineEnding'
}
