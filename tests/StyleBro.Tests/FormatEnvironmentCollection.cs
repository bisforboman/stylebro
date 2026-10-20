namespace StyleBro.Tests;

/// <summary>
/// Test classes that run 'bro format' or the preview, which set and restore process-wide environment variables (the kept
/// findings file, <see cref="StyleBro.Analyzers.KeptFinding.Variable"/>): one at a time, else one class's run reset the
/// variable while another's fake 'dotnet format' wrote to it (trial 36, CI: ArgumentNullException in KeptFindingsTests).
/// </summary>
[CollectionDefinition(Name)]
public sealed class FormatEnvironmentCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Format environment";
}
