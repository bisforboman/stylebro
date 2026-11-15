using System.Runtime.CompilerServices;

namespace StyleBro.Tests;

/// <summary>
/// Gives the tests' temporary folders the repository's global.json. Many tests write a small repository to the temporary
/// folder and run <c>dotnet</c> there; outside the repository its global.json doesn't apply, so those commands ran on
/// the newest SDK installed (11.0.100 on the runner image of 2026-11-11) while the build ran on SDK 10, and guards for
/// SDK 10's behaviour could no longer fail a test
/// (docs/decisions/2026-11-15-stylebros-own-build-and-tests-run-on-sdk-10.md).
/// </summary>
internal static class TestSdk
{
    /// <summary>Points the process's temporary folder at one that holds a copy of the repository's global.json.</summary>
    [ModuleInitializer]
    internal static void UseTheRepositorysSdk()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "StyleBro.slnx")))
        {
            root = Path.GetDirectoryName(root);
        }

        if (root is null || !File.Exists(Path.Combine(root, "global.json")))
        {
            return;
        }

        var temp = Path.Combine(Path.GetTempPath(), "stylebro-tests");
        Directory.CreateDirectory(temp);
        File.Copy(Path.Combine(root, "global.json"), Path.Combine(temp, "global.json"), overwrite: true);
        foreach (var name in new[] { "TMPDIR", "TMP", "TEMP" })
        {
            Environment.SetEnvironmentVariable(name, temp);
        }
    }
}
