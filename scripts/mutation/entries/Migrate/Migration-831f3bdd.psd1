# Trial 22 (kafkaflow): only multi-line switch expressions count (BRO1401 checks those)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& lines.GetLineFromPosition(s.OpenBraceToken.SpanStart).LineNumber != lines.GetLineFromPosition(s.CloseBraceToken.SpanStart).LineNumber);'
    Replace = ');'
    Tests = 'MigrationTests.Migrate_StyleCop11_KeepsTrailingCommas_WhenTheSwitchesHaveThem'
}
