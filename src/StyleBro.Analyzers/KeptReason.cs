namespace StyleBro.Analyzers;

/// <summary>
/// Why a code fix leaves a finding on purpose. The analyzer doesn't report what it can see is unsafe; these are the
/// reasons only the fix can see (other projects, the whole solution), so the warning stays and nothing is changed.
/// 'stylebro-migrate format' lists them (see <see cref="KeptFinding"/>).
/// </summary>
public enum KeptReason
{
    /// <summary>The name is in a string somewhere in the solution (reflection, serializers, test JSON).</summary>
    NameInString,

    /// <summary>The name is in a <c>nameof(...)</c>, whose text would change with it.</summary>
    NameInNameof,

    /// <summary>Another property of the type keeps its name (it's in a string), so the type's properties stay alike.</summary>
    OtherPropertyKept,

    /// <summary>A related member (override, implementation, parameter) is public API.</summary>
    PublicApi,

    /// <summary>It also overrides or implements a member that isn't renamed (another interface's, a library's).</summary>
    ImplementsAnotherMember,

    /// <summary>A derived type already has a member with the new name.</summary>
    DerivedMemberHasNewName,

    /// <summary>The new name is already taken (in the member, the namespace, or a related declaration).</summary>
    NewNameTaken,

    /// <summary>At a use of the name, the new name would mean something else.</summary>
    NewNameMeansSomethingElse,

    /// <summary>A use is in generated code (a source generator, Razor, a designer file), which a tool writes again.</summary>
    GeneratedReference,

    /// <summary>A use doesn't spell the name itself (an alias), so it can't be edited safely.</summary>
    ReferenceNotByName,

    /// <summary>The rename would add compile errors.</summary>
    AddsErrors,

    /// <summary>The name is in disabled '#if' code, which can't be checked.</summary>
    DisabledCode,

    /// <summary>Part of the namespace comes from an assembly outside the solution.</summary>
    NamespaceFromOutside,

    /// <summary>A type in another project derives from the type (InternalsVisibleTo).</summary>
    DerivedTypeInAnotherProject,

    /// <summary>
    /// Code in a project this 'stylebro-migrate format' run doesn't load uses the name (a multi-targeted library's run for a
    /// framework its tests don't target): renaming would leave that use behind.
    /// </summary>
    UsedInProjectNotLoaded,

    /// <summary>No declaration of the name can be renamed (public API, names a library prescribes).</summary>
    NothingToRename,
}

/// <summary>
/// One finding a fix kept, as a line of the file named by <see cref="Variable"/> (tab-separated: path, line, column,
/// rule, reason, message, and where the reason was found when the fix knows it). The fixes append to it when the variable is set; 'stylebro-migrate format' sets it and reads it.
/// </summary>
public sealed class KeptFinding
{
    /// <summary>The environment variable that names the file the fixes append kept findings to.</summary>
    public const string Variable = "STYLEBRO_KEPT_FINDINGS";

    /// <summary>Initializes a new instance of the <see cref="KeptFinding"/> class.</summary>
    public KeptFinding(string path, int line, int column, string id, KeptReason reason, string message, string? where = null)
    {
        Where = where;
        Path = path;
        Line = line;
        Column = column;
        Id = id;
        Reason = reason;
        Message = message;
    }

    /// <summary>Gets the file.</summary>
    public string Path { get; }

    /// <summary>Gets the line (1-based).</summary>
    public int Line { get; }

    /// <summary>Gets the column (1-based).</summary>
    public int Column { get; }

    /// <summary>Gets the rule.</summary>
    public string Id { get; }

    /// <summary>Gets why the fix left it.</summary>
    public KeptReason Reason { get; }

    /// <summary>Gets the diagnostic's message.</summary>
    public string Message { get; }

    /// <summary>Gets where the reason was found ('path(line)' of the first string with the name), or null.</summary>
    public string? Where { get; }

    /// <summary>What a reason means, for people.</summary>
    public static string Describe(KeptReason reason) => reason switch
    {
        KeptReason.NameInString => "the name is in a string in the solution (reflection, serialization): renaming would compile but break at run time",
        KeptReason.NameInNameof => "the name is used in nameof(...), so the rename would change that string too",
        KeptReason.OtherPropertyKept => "another property of the type keeps its name (it's in a string), so all of them keep theirs",
        KeptReason.PublicApi => "a related override, implementation or parameter is public API (stylebro_rename_public_api)",
        KeptReason.ImplementsAnotherMember => "it also overrides or implements a member that isn't renamed",
        KeptReason.DerivedMemberHasNewName => "a derived type already has a member with the new name",
        KeptReason.NewNameTaken => "the new name is already taken",
        KeptReason.NewNameMeansSomethingElse => "at a use of the name, the new name would mean something else",
        KeptReason.GeneratedReference => "it's used in generated code (source generator, Razor, designer file), which a tool writes again",
        KeptReason.ReferenceNotByName => "a use doesn't spell the name (an alias)",
        KeptReason.AddsErrors => "the rename would add compile errors",
        KeptReason.DisabledCode => "the name is in disabled #if code, which can't be checked",
        KeptReason.NamespaceFromOutside => "part of the namespace comes from an assembly outside the solution",
        KeptReason.DerivedTypeInAnotherProject => "a type in another project derives from the type",
        KeptReason.UsedInProjectNotLoaded => "a project that doesn't target all of this one's frameworks uses the name, and no format run loads both, so the rename would miss that use",
        _ => "no declaration can be renamed (public API, or names a library prescribes)",
    };

    /// <summary>A line of the file, or null when it isn't one.</summary>
    public static KeptFinding? Parse(string line)
    {
        var parts = line.Split('\t');
        return parts.Length is 6 or 7
            && int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number)
            && int.TryParse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var column)
            && System.Enum.TryParse<KeptReason>(parts[4], out var reason)
            ? new KeptFinding(parts[0], number, column, parts[3], reason, parts[5], parts.Length == 7 ? parts[6] : null)
            : null;
    }

    /// <summary>The line written to the file.</summary>
    public override string ToString() =>
        string.Join("\t", Path, Line.ToString(System.Globalization.CultureInfo.InvariantCulture), Column.ToString(System.Globalization.CultureInfo.InvariantCulture), Id, Reason.ToString(), Message.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '))
            + (Where is null ? string.Empty : "\t" + Where.Replace('\t', ' '));
}
