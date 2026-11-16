using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace StyleBro.Analyzers.Naming;

/// <summary>What an element of a tuple literal ('(count: 1, 2)') is to the type the literal ends up as.</summary>
internal enum TupleLiteralElement
{
    /// <summary>The literal keeps its own type ('var t = (count: 1, 2)'): the name is the element's, and uses bind to it ('t.count').</summary>
    Own,

    /// <summary>
    /// The literal is converted to a tuple type declared elsewhere that names the element the same ('(int count, int b) t =
    /// (count: 1, 2)'): the name has to follow that declaration's (else CS8123), and is renamed with it.
    /// </summary>
    FollowsTarget,

    /// <summary>
    /// The literal is converted to a tuple type declared elsewhere that names the element differently or not at all
    /// ('(int a, int b) t = (count: 1, 2)'): the compiler ignores the name (CS8123), nothing binds to it.
    /// </summary>
    Ignored,
}

/// <summary>
/// BRO1311 (SA1316): element names in tuple types and explicit names in tuple literals use the configured casing,
/// PascalCase by default ('(int Count, string Name)', '(Count: 1, Name: "a")'). Like StyleCop only the first letter
/// counts, '_' is fine, and names a member has to take from a library member it overrides or implements aren't reported.
/// An override of a member in the solution is reported: the fix renames both. A literal's name that follows a declared
/// tuple type's (<see cref="TupleLiteralElement.FollowsTarget"/>) isn't reported on its own: the declaration's rename
/// takes it along.
/// </summary>
internal static class TupleElementNames
{
    public const string CasingKey = "stylebro_tuple_element_name_casing";
    public const string OldNameKey = "OldName";

    /// <summary>Whether names are camelCase ('camelCase'); PascalCase otherwise, StyleCop's default.</summary>
    public static bool IsCamelCase(AnalyzerConfigOptions options) =>
        options.TryGetValue(CasingKey, out var value) && value.Trim().Equals("camelCase", StringComparison.OrdinalIgnoreCase);

    /// <summary>The name with its first letter in the right case, or null when it's right already (or can't be fixed).</summary>
    public static string? GetNewName(string name, bool camelCase)
    {
        if (name.Length == 0 || name == "_" || !char.IsLetter(name[0]) || char.IsLower(name[0]) == camelCase)
        {
            return null;
        }

        var newName = (camelCase ? char.ToLowerInvariant(name[0]) : char.ToUpperInvariant(name[0])) + name.Substring(1);
        return SyntaxFacts.GetKeywordKind(newName) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(newName) == SyntaxKind.None
            ? newName
            : null;
    }

    /// <summary>
    /// Whether the element is in the signature of a member that overrides or implements a library member whose own
    /// declaration names the tuple elements: the names have to match (CS8139), and the library can't be renamed (StyleCop's
    /// CanTupleElementNameBeChanged). 'IComparer&lt;(int a, int b)&gt;.Compare' takes its names from the type argument,
    /// which is in the solution, so that one can be renamed. A base member in the solution counts too when it is public API
    /// and <see cref="PublicApi.RenameKey"/> is false: its names aren't renamed, so an internal implementer's can't be either
    /// (Serde's 'ITypeDeserializer', trial 26: reported, then kept for the compile errors every time).
    /// </summary>
    public static bool InheritsNames(TupleElementSyntax element, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        return element.FirstAncestorOrSelf<MemberDeclarationSyntax>() is { } member
            && IsInSignature(element, member)
            && model.GetDeclaredSymbol(member, cancellationToken) is { } symbol
            && CamelCaseNamingAnalyzer.GetBaseMembers(symbol).Any(b =>
                (!b.Locations.Any(l => l.IsInSource) || !PublicApi.CanRename(b, options)) && HasTupleNames(b.OriginalDefinition));
    }

    /// <summary>
    /// Whether the element is in the signature of a member other assemblies see (<see cref="PublicApi.IsVisible"/>): a
    /// method's, operator's, property's, indexer's or delegate's return or parameter types, a field's or event's type, or
    /// a type's header (record and primary constructor parameters, base types, constraints). Such an element is renamed
    /// only when <see cref="PublicApi.RenameKey"/> is true.
    /// </summary>
    public static bool IsInPublicSignature(TupleElementSyntax element, SemanticModel model, CancellationToken cancellationToken)
    {
        if (element.FirstAncestorOrSelf<MemberDeclarationSyntax>() is not { } member)
        {
            return false;
        }

        SyntaxNode? declared = member switch
        {
            BaseFieldDeclarationSyntax field => field.Declaration.Type.Span.Contains(element.Span) ? field.Declaration.Variables.FirstOrDefault() : null,
            DelegateDeclarationSyntax @delegate => @delegate.ReturnType.Span.Contains(element.Span) || @delegate.ParameterList.Span.Contains(element.Span) ? @delegate : null,

            // The type's own header: a record's or primary constructor's parameters, base types, constraints (trial 25).
            TypeDeclarationSyntax type => type,
            _ => IsInSignature(element, member) ? member : null,
        };
        return declared is not null && model.GetDeclaredSymbol(declared, cancellationToken) is { } symbol && PublicApi.IsVisible(symbol);
    }

    /// <summary>Whether the node is in the member's return type, parameter types or property/event type, not its body.</summary>
    public static bool IsInSignature(SyntaxNode node, MemberDeclarationSyntax member)
    {
        TypeSyntax? type = member switch
        {
            MethodDeclarationSyntax m => m.ReturnType,
            OperatorDeclarationSyntax o => o.ReturnType,
            ConversionOperatorDeclarationSyntax c => c.Type,
            BasePropertyDeclarationSyntax p => p.Type,
            _ => null,
        };
        var parameters = member switch
        {
            BaseMethodDeclarationSyntax m => m.ParameterList,
            IndexerDeclarationSyntax i => (BaseParameterListSyntax)i.ParameterList,
            _ => null,
        };
        return (type is not null && type.Span.Contains(node.Span)) || (parameters is not null && parameters.Span.Contains(node.Span));
    }

    /// <summary>
    /// What the element at <paramref name="index"/> of a tuple literal is to the type the literal ends up as, and that
    /// type's element (null when the literal has no tuple type at all). The literal's own type's elements are declared
    /// in the literal (at the explicit name, or at the argument an inferred name comes from); a declared target's
    /// elements are declared somewhere else (or in a library).
    /// </summary>
    public static (TupleLiteralElement Kind, IFieldSymbol? Element) ClassifyLiteralElement(TupleExpressionSyntax tuple, int index, SemanticModel model, CancellationToken cancellationToken)
    {
        var argument = tuple.Arguments[index];
        bool IsOwn(IFieldSymbol element) => element.Locations.Any(l => l.SourceTree == tuple.SyntaxTree && argument.Span.Contains(l.SourceSpan));

        var info = model.GetTypeInfo(tuple, cancellationToken);
        var element = GetElement(info.ConvertedType, index);
        if (element is null || IsOwn(element))
        {
            // The conversion as the operation tree has it, for what the type info doesn't show as one.
            if (GetElement(GetTargetType(tuple, model, cancellationToken), index) is not { } target || IsOwn(target))
            {
                return (TupleLiteralElement.Own, element ?? GetElement(info.Type, index));
            }

            element = target;
        }

        var name = argument.NameColon?.Name.Identifier.ValueText ?? GetInferredName(argument.Expression);
        return (element.Name == name ? TupleLiteralElement.FollowsTarget : TupleLiteralElement.Ignored, element);
    }

    /// <summary>
    /// Whether another element of the literal has the name already, written or inferred ('(count: 1, Count: 2)', '(count:
    /// 1, other.Count)'): a second 'Count' is an error (CS8127), or silently takes the inferred name away, and 't.Count'
    /// would then mean the other element. Or the name is one of the tuple's own members ('rest' -> 'Rest', 'item2' ->
    /// 'Item2' for the first element: CS8126, CS8125).
    /// </summary>
    public static bool HasNameClash(TupleExpressionSyntax tuple, int index, string newName)
    {
        if (newName is "CompareTo" or "Deconstruct" or "Equals" or "GetHashCode" or "Rest" or "ToString"
            || (newName.StartsWith("Item", StringComparison.Ordinal) && int.TryParse(newName.Substring(4), out var position) && position != index + 1))
        {
            return true;
        }

        for (var i = 0; i < tuple.Arguments.Count; i++)
        {
            if (i != index && (tuple.Arguments[i].NameColon?.Name.Identifier.ValueText ?? GetInferredName(tuple.Arguments[i].Expression)) == newName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The name a tuple literal's element takes from its expression ('pair.first', 'first', 'pair?.first'), or null.</summary>
    public static string? GetInferredName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        ConditionalAccessExpressionSyntax conditional => GetInferredName(conditional.WhenNotNull),
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        _ => null,
    };

    /// <summary>
    /// The type a tuple literal is converted to, from its context in the operation tree, or the type of what it's compared
    /// with ('pair == (count: 1, 2)': a name the other side doesn't have is ignored with a warning, CS8383; another
    /// literal there is no declared type); null where the context isn't one of these.
    /// </summary>
    private static ITypeSymbol? GetTargetType(TupleExpressionSyntax tuple, SemanticModel model, CancellationToken cancellationToken)
    {
        var operation = model.GetOperation(tuple, cancellationToken);
        while (operation?.Parent is IConversionOperation or IParenthesizedOperation)
        {
            operation = operation.Parent;
        }

        return operation?.Parent switch
        {
            IConversionOperation conversion => conversion.Type,
            ISimpleAssignmentOperation assignment => assignment.Target.Type,
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } => declarator.Symbol.Type,
            IFieldInitializerOperation field => field.InitializedFields.FirstOrDefault()?.Type,
            IPropertyInitializerOperation property => property.InitializedProperties.FirstOrDefault()?.Type,
            IArgumentOperation argument => argument.Parameter?.Type,
            IReturnOperation => ReturnType(model.GetEnclosingSymbol(tuple.SpanStart, cancellationToken) as IMethodSymbol),
            ITupleBinaryOperation comparison => ComparedType(comparison.LeftOperand == operation ? comparison.RightOperand : comparison.LeftOperand),
            _ => null,
        };
    }

    private static ITypeSymbol? ComparedType(IOperation other)
    {
        while (other is IConversionOperation or IParenthesizedOperation)
        {
            other = other is IConversionOperation conversion ? conversion.Operand : ((IParenthesizedOperation)other).Operand;
        }

        return other is ITupleOperation ? null : other.Type;
    }

    private static ITypeSymbol? ReturnType(IMethodSymbol? method) =>
        method is { IsAsync: true, ReturnType: INamedTypeSymbol { TypeArguments.Length: 1 } task } ? task.TypeArguments[0] : method?.ReturnType;

    /// <summary>The tuple type's element at <paramref name="index"/> (a nullable tuple's too), or null.</summary>
    private static IFieldSymbol? GetElement(ITypeSymbol? type, int index)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        return type is INamedTypeSymbol { IsTupleType: true } tuple && index < tuple.TupleElements.Length ? tuple.TupleElements[index] : null;
    }

    /// <summary>Whether a member's own signature (before type arguments are filled in) names tuple elements.</summary>
    private static bool HasTupleNames(ISymbol member) => member switch
    {
        IMethodSymbol method => HasTupleNames(method.ReturnType) || method.Parameters.Any(p => HasTupleNames(p.Type)),
        IPropertySymbol property => HasTupleNames(property.Type) || property.Parameters.Any(p => HasTupleNames(p.Type)),
        IEventSymbol e => HasTupleNames(e.Type),
        _ => false,
    };

    private static bool HasTupleNames(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements.Any(e => e.IsExplicitlyNamedTupleElement || HasTupleNames(e.Type)),
        INamedTypeSymbol named => named.TypeArguments.Any(HasTupleNames),
        IArrayTypeSymbol array => HasTupleNames(array.ElementType),
        _ => false,
    };
}
