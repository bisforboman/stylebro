# bro format: kept findings don't fail it, files left to change do (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.Where(l => !IsKeptLine(l, keys) && !IsWhitespaceLine(l))'
    Replace = 'lines.Where(l => !IsWhitespaceLine(l))'
    Tests = 'KeptFindingsTests'
}
