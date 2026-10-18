namespace StyleBro.Analyzers;

/// <summary>
/// The preset versions (docs/versioning.md): the package's build targets add one global config per minor release up to
/// &lt;StyleBroPreset&gt;, so rules added in a newer minor stay off until a team raises it.
/// </summary>
public static class PresetVersions
{
    /// <summary>
    /// The newest preset layer in the package: what 'latest' means, and what 'bro init' writes. Raised with a
    /// minor's first new rule; PresetVersionTests checks it against the layer files and the build targets.
    /// </summary>
    public const string Latest = "1.0";
}
