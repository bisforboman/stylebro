# bro format: kept findings don't fail it, files left to change do (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.Where(l => !IsKeptLine(l, kept) && !IsLineEndingLine(l))'
    Replace = 'lines.Where(l => !IsLineEndingLine(l))'
    Tests = 'KeptFindingsTests'
}
