# Sonar mapping: a narrowing stylebro_ setting isn't written when the StyleCop setup turns the rule on.
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '&& lines.Any(l => KeyOf(l) == key && (StyleCopSetup.ParseSeverity(ValueOf(l)) ?? Severity.None) > Severity.None)).ToList();'
    Replace = '&& false).ToList();'
    Tests = 'MigrationTests'
}
