# bro format: kept findings don't fail it, files left to change do (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (copyCode != 0 || changed > 0)'
    Replace = 'if (copyCode != 0)'
    Tests = 'KeptFindingsTests'
}
