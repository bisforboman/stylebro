# trial 49: SDK 8's dotnet-format (MSBuildLocator) isn't for the host
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = ': !File.Exists(Path.Combine(folder, "Microsoft.Build.dll")) ?'
    Replace = ': false ?'
    Tests = 'FormatHostTests.AnSdkWhoseDotnetFormatUsesMSBuildLocator_IsNotForTheHost'
}
