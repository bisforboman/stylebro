# Format host: an option the host doesn't know goes back to dotnet format
@{
    File = 'src/StyleBro.FormatHost/FormatArguments.cs'
    Find = '                default:
                    return null;'
    Replace = '                default:
                    break;'
    Tests = 'FormatHostTests.Arguments_TheHostDoesntRun_AreDeclined'
}
