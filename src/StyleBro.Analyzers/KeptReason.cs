namespace StyleBro.Analyzers;

/// <summary>
/// Why a code fix leaves a finding on purpose. The analyzer doesn't report what it can see is unsafe; these are the
/// reasons only the fix can see (other projects, the whole solution), so the warning stays and nothing is changed.
/// 'bro format' lists them (see <see cref="KeptFinding"/>).
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
    /// Code this 'bro format' run doesn't load uses the name: a project formatted in another run (a
    /// multi-targeted library's run for a framework its tests don't target, projects sharing files), or a file no loaded
    /// project compiles. Renaming would leave that use behind.
    /// </summary>
    UsedInProjectNotLoaded,

    /// <summary>No declaration of the name can be renamed (public API, names a library prescribes).</summary>
    NothingToRename,

    /// <summary>Code reads the type's members by position through reflection (<c>GetFields()[0]</c>), so sorting would change what it reads.</summary>
    MembersReadByPosition,

    /// <summary>
    /// The rename would end a clash between a tuple element and a name in scope, so IDE0042 (deconstruct variable
    /// declaration), a warning or an error here, would report the tuple's variable.
    /// </summary>
    AddsDeconstruction,

    /// <summary>
    /// Another element of the same tuple type keeps its name (named in <see cref="KeptFinding.Where"/> with its reason), and
    /// a tuple type's elements are renamed together, so this one keeps its own too.
    /// </summary>
    KeptWithOtherElement,

    /// <summary>Code uses the name through <see langword="dynamic"/> (<c>((dynamic)x).Name</c>), which binds by name at run time.</summary>
    UsedThroughDynamic,

    /// <summary>The name is in a snapshot or approval file ('*.verified.*', '*.received.*', '*.approved.*'), which a serializer wrote.</summary>
    NameInSnapshotFile,

    /// <summary>Visual Basic code uses the name: StyleBro leaves VB code alone.</summary>
    UsedFromVisualBasic,

    /// <summary>A XAML file uses the name (a binding path, 'x:Name', an event handler), which binds it by name at run time.</summary>
    NameInXamlFile,

    /// <summary>A XAML file names it as an element or attribute ('&lt;local:Badge caption="hi"/&gt;'), which the XAML build resolves.</summary>
    NameInXamlMarkup,

    /// <summary>A derived type hides the member with a 'new' member of the same name, which would then hide nothing (CS0109).</summary>
    HiddenByNewMember,

    /// <summary>
    /// The name is in an interpolated string's hole whose handler takes the hole's text as a <c>[CallerArgumentExpression]</c>
    /// argument (ZLogger's structured-log keys), so the text is data at run time.
    /// </summary>
    NameIsLogKey,

    /// <summary>
    /// A static field that registers a XAML framework's property or routed event (<c>LevelProperty</c> for the property
    /// <c>Level</c>): XAML finds it by that name.
    /// </summary>
    RegistrationField,

    /// <summary>
    /// The project uses Shouldly, whose failure messages quote the call site's code, and a string in the file has the name
    /// (an expected message: "`methodCall()` should not throw").
    /// </summary>
    NameInShouldlyMessage,

    /// <summary>
    /// The documentation text the fix would change is a string, or is in a snapshot file, in the solution: code reads XML docs
    /// at run time (NSwag's and Swashbuckle's OpenAPI descriptions) and a test compares the text.
    /// </summary>
    DocumentationTextInString,

    /// <summary>
    /// The parameter has an attribute of its own (<c>[Argument]</c>, <c>[Option]</c>, <c>[FromQuery]</c>): frameworks that
    /// attribute parameters bind them by name at run time (Cocona's command line names, trial 49).
    /// </summary>
    ParameterHasAttribute,
}

/// <summary>
/// One finding a fix kept, as a line of the file named by <see cref="Variable"/> (tab-separated: path, line, column,
/// rule, reason, message, and where the reason was found when the fix knows it). The fixes append to it when the variable is set; 'bro format' sets it and reads it.
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
        KeptReason.OtherPropertyKept => "another property of the type keeps its name (it's in a string, a snapshot file or a XAML file, or it's snake_case), so all of them keep theirs",
        KeptReason.UsedThroughDynamic => "code uses the name through 'dynamic', which binds it by name at run time",
        KeptReason.NameInSnapshotFile => "the name is in a snapshot or approval file (*.verified.*, *.received.*, *.approved.*): renaming would fail those tests",
        KeptReason.NameInShouldlyMessage => "the project uses Shouldly, whose failure messages quote the code, and a string in the file has the name (an expected message): renaming would fail that test",
        KeptReason.DocumentationTextInString => "the documentation text is a string or is in a snapshot file in the solution (code that reads XML docs at run time, such as an OpenAPI generator, and a test comparing the text): changing it would fail that test",
        KeptReason.ParameterHasAttribute => "the parameter has an attribute ([Argument], [Option], [FromQuery], ...), and frameworks that attribute parameters read their names at run time (command line or query names): renaming would compile but change what callers use",
        KeptReason.UsedFromVisualBasic => "Visual Basic code uses the name, and StyleBro doesn't edit VB code",
        KeptReason.NameInXamlFile => "a XAML file uses the name (a binding path, x:Name, an event handler), which binds it by name at run time: renaming would break the binding silently",
        KeptReason.NameInXamlMarkup => "a XAML file names it as an element or attribute (a type or property of the project's own namespace): renaming would break the XAML build (MC3074, MC3072)",
        KeptReason.HiddenByNewMember => "a derived type hides it with a 'new' member of the same name, which would then hide nothing (CS0109)",
        KeptReason.NameIsLogKey => "the name is a log key: it's in an interpolated string's hole whose handler takes the hole's text as a [CallerArgumentExpression] argument (structured logging, e.g. ZLogger), so renaming would change the logged data",
        KeptReason.RegistrationField => "it registers a XAML framework's property or routed event (Avalonia, WPF, MAUI, WinUI), whose field XAML and the framework's analyzers find by the '<Name>Property' or '<Name>Event' name",
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
        KeptReason.UsedInProjectNotLoaded => "code this format run doesn't load (another run's project, or a file outside the loaded projects) uses the name, so the rename would miss that use",
        KeptReason.MembersReadByPosition => "code reads the type's members by position through reflection (GetFields()[0]), so sorting them would change what it reads",
        KeptReason.KeptWithOtherElement => "another element of the same tuple type keeps its name, and a tuple type's elements are renamed together",
        KeptReason.AddsDeconstruction => "a tuple element would no longer share its name with something in scope, so IDE0042 (deconstruct variable), a warning or an error here, would report the tuple's variable",
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
        string.Join("\t", Path, Line.ToString(System.Globalization.CultureInfo.InvariantCulture), Column.ToString(System.Globalization.CultureInfo.InvariantCulture), Id, Reason.ToString(), Printable(Message.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')))
            + (Where is null ? string.Empty : "\t" + Printable(Where.Replace('\t', ' ')));

    /// <summary>The text with each lone surrogate as U+FFFD: the line is appended as UTF-8, which can't hold one.</summary>
    private static string Printable(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsHighSurrogate(chars[i]) && i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(chars[i]))
            {
                chars[i] = '�';
            }
        }

        return new string(chars);
    }
}
