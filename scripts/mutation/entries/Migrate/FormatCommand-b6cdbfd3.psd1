# Linked folders outside the repository stay someone else's (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(Path.TrimEndingDirectorySeparator(f) + Path.DirectorySeparatorChar).StartsWith(top, StringComparison.OrdinalIgnoreCase) && '
    Replace = ''
    Tests = 'LinkedFolders_OutsideTheRepository'
}
