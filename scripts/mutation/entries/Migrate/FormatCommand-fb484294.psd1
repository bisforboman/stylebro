# --report followed by another option has no path (check's own --verify-no-changes follows it).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return at >= 0 && (at + 1 >= args.Count || args[at + 1].StartsWith(''-''));'
    Replace = 'return at >= 0 && at + 1 >= args.Count;'
    Tests = 'ReportWithoutAPath_IsRefused'
}
