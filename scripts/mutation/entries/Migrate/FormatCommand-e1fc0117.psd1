# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (match.Groups["name"].Value == "net" && match.Groups["digits"].Value[0] < ''5'')'
    Replace = ''
    Tests = 'MigrationTests.Format_NormalizesFrameworkSpellings'
}
