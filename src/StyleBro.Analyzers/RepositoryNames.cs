using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers;

/// <summary>
/// The names in a repository's C# files that the renaming fixes check beyond the solution they see: string literals
/// (reflection: <c>GetField("count")</c>), <see langword="nameof"/> names, and each file's identifiers. 'stylebro-migrate format'
/// formats a multi-targeted repository one target framework at a time, and a run loads only the projects that target
/// that framework: Scrutor's netstandard2.0 run loaded the library without its tests, which read a private field with
/// GetField, and the field was renamed. So format writes these names for every C# file to the file
/// <see cref="Variable"/> names, and the fixes check them too (one line per entry, see <see cref="Lines"/>).
/// </summary>
public static class RepositoryNames
{
    /// <summary>The environment variable that names the file; not set (the IDE, plain 'dotnet format'), nothing extra is checked.</summary>
    public const string Variable = "STYLEBRO_REPOSITORY_NAMES";

    /// <summary>A string literal's text (<see cref="Entry.Where"/>: 'path(line)').</summary>
    public const char StringKind = 'S';

    /// <summary>A name in <c>nameof(...)</c>.</summary>
    public const char NameofKind = 'N';

    /// <summary>A file's identifiers, space-separated (<see cref="Entry.Where"/>: the path).</summary>
    public const char IdentifierKind = 'I';

    /// <summary>
    /// A type whose members code reads by position (<see cref="Ordering.ReflectionByPosition"/>): the type's simple name and
    /// the method, space-separated (<see cref="Entry.Where"/>: 'path(line)').
    /// </summary>
    public const char PositionKind = 'P';

    /// <summary>
    /// A string literal that names a member for code that reaches it by name (<see cref="IsReflectionShaped"/>), listed as a
    /// <see cref="StringKind"/> too (<see cref="Entry.Where"/>: 'path(line)').
    /// </summary>
    public const char ReflectionKind = 'R';

    // Type's (and TypeInfo's) methods that take a member's name. Other types have methods of the same names
    // (JsonElement.GetProperty("count") in Orleans' tests), so only on a receiver that looks like a Type (TypeLike), or
    // without one (a helper of the same name).
    private static readonly HashSet<string> TypeMethods = new(System.StringComparer.Ordinal)
    {
        "GetField", "GetProperty", "GetMember", "GetMethod", "GetEvent", "GetNestedType", "GetInterface",
    };

    // Methods that take a member's or type's name whatever they're called on: RuntimeReflectionExtensions, TypeInfo,
    // Type.GetType/Assembly.GetType, activation, System.Linq.Expressions, Harmony's AccessTools/Traverse, PrivateObject,
    // EF Core's backing fields ('Property("_name")', 'HasField("_name")'). Besides, any method with "Field" as a word of
    // its name ('GetPrivateField', 'SetFieldValue', a test's own helper).
    private static readonly HashSet<string> ReflectionMethods = new(System.StringComparer.Ordinal)
    {
        "GetRuntimeField", "GetRuntimeProperty", "GetRuntimeMethod", "GetRuntimeEvent",
        "GetDeclaredField", "GetDeclaredProperty", "GetDeclaredMethod", "GetDeclaredMethods", "GetDeclaredEvent", "GetDeclaredNestedType",
        "GetType", "InvokeMember", "CreateInstance", "CreateInstanceFrom", "PropertyOrField", "Property", "Method", "GetFieldOrProperty", "SetFieldOrProperty",
    };

    // Attributes that name a member of the test class or a type: [MemberData("Cases")], [TestCaseSource("cases")],
    // [ValueSource("values")], [DynamicData("Rows")]; any attribute's 'Name = "..."' ([UnsafeAccessor(..., Name = "count")],
    // [DataMember(Name = "...")]).
    private static readonly HashSet<string> SourceAttributes = new(System.StringComparer.Ordinal)
    {
        "MemberData", "DynamicData", "TestCaseSource", "ValueSource",
    };

    /// <summary>
    /// The string literals, <see langword="nameof"/> names and types whose members are read by position in a tree (the text,
    /// and the 0-based line).
    /// </summary>
    public static IEnumerable<(char Kind, string Text, int Line)> Collect(SyntaxNode root)
    {
        foreach (var token in TreeWalk.Tokens(root))
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                var line = token.GetLocation().GetLineSpan().StartLinePosition.Line;
                yield return (StringKind, token.ValueText, line);
                if (IsReflectionShaped(token))
                {
                    yield return (ReflectionKind, token.ValueText, line);
                }
            }
            else if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"
                && token.Parent?.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation)
            {
                yield return (NameofKind, invocation.ArgumentList.Arguments[0].Expression.GetLastToken().ValueText, 0);
            }
            else if (token.IsKind(SyntaxKind.IdentifierToken) && Ordering.ReflectionByPosition.GetReadType(token) is { } type)
            {
                yield return (PositionKind, type + " " + token.ValueText, token.GetLocation().GetLineSpan().StartLinePosition.Line);
            }
        }
    }

    /// <summary>The lines for one file: its strings and nameof names, then one line with its identifiers.</summary>
    public static IEnumerable<string> Lines(SyntaxNode root, string path)
    {
        foreach (var (kind, text, line) in Collect(root))
        {
            yield return kind is StringKind or PositionKind or ReflectionKind ? Line(kind, $"{path}({line + 1})", text) : Line(kind, string.Empty, text);
        }

        var identifiers = new SortedSet<string>(TreeWalk.Tokens(root).Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText), System.StringComparer.Ordinal);
        yield return Line(IdentifierKind, path, string.Join(" ", identifiers));
    }

    /// <summary>
    /// Whether a string literal is where code names a member to reach it by name: an argument of a reflection method
    /// (<c>typeof(C).GetField("count")</c>, <c>GetType().GetProperty(...)</c>, <c>Type.GetType("App.C")</c>,
    /// <c>Expression.Field(x, "count")</c>, a helper with "Field" in its name), a member-source attribute's argument, or an
    /// attribute's <c>Name = "..."</c>. Syntax only: the repository-names file is written without a compilation. Any other
    /// string ('Assert.Equal("options", e.ParamName)', a JSON key) doesn't block a private field's rename (owner's decision,
    /// docs/decisions.md): in Orleans common words in messages kept 692 renames.
    /// </summary>
    public static bool IsReflectionShaped(SyntaxToken token)
    {
        if (!token.IsKind(SyntaxKind.StringLiteralToken) || token.Parent is not LiteralExpressionSyntax literal)
        {
            return false;
        }

        switch (literal.Parent)
        {
            case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }:
                var (name, receiver) = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax access => (access.Name.Identifier.ValueText, access.Expression),
                    MemberBindingExpressionSyntax binding => (binding.Name.Identifier.ValueText, binding.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression),
                    SimpleNameSyntax simple => (simple.Identifier.ValueText, null),
                    _ => (string.Empty, null),
                };
                return TypeMethods.Contains(name)
                    ? receiver is null || IsTypeLike(receiver)
                    : ReflectionMethods.Contains(name) || HasFieldWord(name);

            case AttributeArgumentSyntax argument:
                if (argument.NameEquals?.Name.Identifier.ValueText == "Name")
                {
                    return true;
                }

                var attribute = argument.FirstAncestorOrSelf<AttributeSyntax>()?.Name.GetLastToken().ValueText ?? string.Empty;
                attribute = attribute.EndsWith("Attribute", System.StringComparison.Ordinal) ? attribute.Substring(0, attribute.Length - "Attribute".Length) : attribute;
                return argument.NameEquals is null && SourceAttributes.Contains(attribute);

            default:
                return false;
        }
    }

    /// <summary>A line of the file, or null when it isn't one.</summary>
    public static Entry? Parse(string line)
    {
        var first = line.IndexOf('\t');
        var second = first < 0 ? -1 : line.IndexOf('\t', first + 1);
        return first == 1 && second > 0 ? new Entry(line[0], Unescape(line.Substring(2, second - 2)), Unescape(line.Substring(second + 1))) : null;
    }

    /// <summary>
    /// Whether an expression looks like a <see cref="System.Type"/>: <c>typeof(C)</c>, <c>x.GetType()</c>,
    /// <c>t.GetTypeInfo()</c>, or a name ending in "type" or "TypeInfo" (<c>entityType</c>, <c>type.BaseType</c>).
    /// </summary>
    private static bool IsTypeLike(ExpressionSyntax receiver)
    {
        while (receiver is PostfixUnaryExpressionSyntax or ParenthesizedExpressionSyntax)
        {
            receiver = receiver is PostfixUnaryExpressionSyntax postfix ? postfix.Operand : ((ParenthesizedExpressionSyntax)receiver).Expression;
        }

        var last = receiver switch
        {
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax call } => call.Name.Identifier.ValueText,
            InvocationExpressionSyntax { Expression: SimpleNameSyntax call } => call.Identifier.ValueText,
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => string.Empty,
        };
        return receiver is TypeOfExpressionSyntax
            || (receiver is InvocationExpressionSyntax && last is "GetType" or "GetTypeInfo" or "AsType" or "MakeGenericType" or "GetGenericTypeDefinition")
            || (receiver is not InvocationExpressionSyntax
                && (last.EndsWith("type", System.StringComparison.OrdinalIgnoreCase) || last.EndsWith("TypeInfo", System.StringComparison.Ordinal)));
    }

    /// <summary>Whether "Field" or "Fields" is a word of a PascalCase name ('GetPrivateField', 'SetFieldValue'; not 'Fieldset').</summary>
    private static bool HasFieldWord(string name)
    {
        for (var at = name.IndexOf("Field", System.StringComparison.Ordinal); at >= 0; at = name.IndexOf("Field", at + 1, System.StringComparison.Ordinal))
        {
            var end = at + "Field".Length;
            end += end < name.Length && name[end] == 's' ? 1 : 0;
            if (end == name.Length || char.IsUpper(name[end]))
            {
                return true;
            }
        }

        return false;
    }

    private static string Line(char kind, string where, string text) => kind + "\t" + Escape(where) + "\t" + Escape(text);

    /// <summary>
    /// Backslash, tab and line breaks escaped, and a lone surrogate as <c>\uD800</c>: UTF-8 can't hold one (Orleans' tests have
    /// <c>"jobs/\uD800"</c>, and File.WriteAllLines threw before any formatting). <see cref="Unescape"/> gives the text back.
    /// </summary>
    private static string Escape(string text)
    {
        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                result.Append(c).Append(text[++i]);
            }
            else if (char.IsSurrogate(c))
            {
                result.Append("\\u").Append(((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                result.Append(c switch { '\\' => "\\\\", '\t' => "\\t", '\r' => "\\r", '\n' => "\\n", _ => c.ToString() });
            }
        }

        return result.ToString();
    }

    private static string Unescape(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 5 < text.Length && text[i + 1] == 'u'
                && int.TryParse(text.Substring(i + 2, 4), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out var code))
            {
                result.Append((char)code);
                i += 5;
            }
            else if (text[i] == '\\' && i + 1 < text.Length)
            {
                i++;
                result.Append(text[i] switch { 't' => '\t', 'r' => '\r', 'n' => '\n', _ => text[i] });
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>One line of the file.</summary>
    public sealed class Entry
    {
        /// <summary>Initializes a new instance of the <see cref="Entry"/> class.</summary>
        public Entry(char kind, string where, string text)
        {
            Kind = kind;
            Where = where;
            Text = text;
        }

        /// <summary>Gets the kind (<see cref="StringKind"/>, <see cref="NameofKind"/>, <see cref="IdentifierKind"/> or <see cref="PositionKind"/>).</summary>
        public char Kind { get; }

        /// <summary>Gets where it is: 'path(line)' for a string or a read by position, the path for identifiers.</summary>
        public string Where { get; }

        /// <summary>Gets the string's text, the name, or the identifiers.</summary>
        public string Text { get; }
    }
}
