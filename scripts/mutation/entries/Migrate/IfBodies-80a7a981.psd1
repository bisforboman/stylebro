# A protected line is found again only when the run kept the number of lines with its text.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (matches.Count != old.Count(l => l.Text.Trim() == key))'
    Replace = 'if (matches.Count == 0)'
    Tests = 'MigrationTests.IfBodies_KeepTheirIndentationAndLineBreak_WhereverTheRunMovedThem'
}
