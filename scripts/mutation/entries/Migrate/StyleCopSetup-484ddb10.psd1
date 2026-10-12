# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '@"\bId\s*=\s*""S[AX]\d{4}""", RegexOptions.IgnoreCase)'
    Replace = '@"\bId\s*=\s*""S[AX]\d{4}""")'
    Tests = 'MigrationTests.LowerCaseStyleCopIds_'
}
