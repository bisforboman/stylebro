# The protected line gets its indentation back.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'var text = index.Indent ? Indentation(old[index.Line].Text) + now[at].Text.TrimStart() : now[at].Text;'
    Replace = 'var text = now[at].Text;'
    Tests = 'MigrationTests.IfBodies_KeepTheirIndentationAndLineBreak_WhereverTheRunMovedThem'
}
