# Format host: a check logs its findings as dotnet format does
@{
    File = 'src/StyleBro.FormatHost/FixLoop.cs'
    Find = '!save || level == LogLevel.Debug'
    Replace = 'level == LogLevel.Debug'
    Tests = 'FormatHostTests.AHostCheck_SaysWhatDotnetFormatSays'
}
