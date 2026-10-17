# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'summary.Written.AddRange(prefixLines.Select(l => l.Substring(0, l.IndexOf(''='')).Trim()));'
    Replace = 'summary.Written.Add(StyleBro.Analyzers.Naming.FieldNames.MutableStaticStyleKey);'
    Tests = 'MigrationTests.Init_NamesBothStaticKeys_InTheSummary'
}
