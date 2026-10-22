# Format host: --engine takes host or dotnet-format only
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Host.cs'
    Find = 'if (value is not (HostEngine or DotnetFormatEngine))'
    Replace = 'if (false)'
    Tests = 'FormatHostTests.TheEngineOption_IsTakenOut'
}
