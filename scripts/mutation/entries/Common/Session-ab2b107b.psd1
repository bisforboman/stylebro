# Format host: phase 1 runs checks only; fixes stay in dotnet format
@{
    File = 'src/StyleBro.FormatHost/Session.cs'
    Find = 'is not { Verify: true } parsed'
    Replace = 'is not { } parsed'
    Tests = 'FormatHostTests.AHostCheck_SaysWhatDotnetFormatSays'
}
