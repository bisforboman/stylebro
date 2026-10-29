# Trial 62: a package's framework support warning goes to the run of that framework.
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '            : frameworks.Count > 0 && UnsupportedFramework(d.Message) is { } unsupported
            ? frameworks.Contains(unsupported, StringComparer.OrdinalIgnoreCase)
'
    Replace = ''
    Tests = 'StyleBro.Tests.FormatHostTests.AFrameworkSupportWarning_GoesToTheRunOfThatFramework'
}
