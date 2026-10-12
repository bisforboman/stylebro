using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Modernize;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1147: a null-forgiving '!' whose operand the compiler already knows is not null (its flow state).
/// Removing it changes no warning: the operand's state and type stay what they were. Only cases where that holds for
/// certain are reported (see <see cref="GetChange"/>).
/// </summary>
internal static class NullForgiving
{
    /// <summary>
    /// A '!' on a value whose declaration has no nullable annotation (a property, field or method return declared under
    /// '#nullable disable' or in an unannotated library, or a 'var' local initialized from one) stays (default false), like
    /// Sonar's S8969: the compiler treats such a value as not null without knowing it.
    /// </summary>
    public const string AllowOnUnannotatedKey = "stylebro_allow_null_forgiving_on_unannotated";

    /// <summary>
    /// The edit that removes the '!' of <paramref name="node"/>, or null when it isn't redundant or is skipped: 'null!' and
    /// 'default!' (intentional), nullable analysis off or changed by '#nullable' from the project's setting, an operand of a
    /// type parameter, an array or a type with type arguments other than plain value types (the '!' also hides nested
    /// nullability mismatches, 'List&lt;string?&gt;' to 'List&lt;string&gt;'), ref/out arguments (their declared types
    /// must match), comments or line breaks before the '!', and projects with several target frameworks (the base
    /// library's annotations differ per framework).
    /// </summary>
    public static TextChange? GetChange(PostfixUnaryExpressionSyntax node, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        var operand = node.Operand;
        if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression)
            || operand is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression }
            || operand is DefaultExpressionSyntax
            || operand.HasTrailingTrivia
            || node.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 }
            || MultiTargetSuppressor.GetFrameworks(options).Count > 1)
        {
            return null;
        }

        // Without nullable warnings the flow state is never not-null: the first check only saves the speculative binding.
        var context = model.GetNullableContext(node.SpanStart);
        var project = model.Compilation.Options.NullableContextOptions;
        if (!context.WarningsEnabled()
            || context.WarningsEnabled() != project.WarningsEnabled()
            || context.AnnotationsEnabled() != project.AnnotationsEnabled())
        {
            return null;
        }

        // The operand's own type info reports the state after the '!' (always not-null), so the '!' is removed in a
        // speculative copy of the enclosing statement, initializer or expression body; the compiler analyzes it from the
        // state the original had there.
        return IsFlat(model.GetTypeInfo(operand, cancellationToken).Type)
            && !ReturnsMaybeNull(operand, model, cancellationToken)
            && !(options.TryGetValue(AllowOnUnannotatedKey, out var allow) && bool.TryParse(allow.Trim(), out var allowed) && allowed
                && IsUnannotated(operand, model, cancellationToken))
            && StateWithout(node, model, cancellationToken) == NullableFlowState.NotNull
            ? new TextChange(node.OperatorToken.Span, string.Empty)
            : null;
    }

    // A call whose return type is annotated, with no attribute that could narrow it, returns maybe-null: the speculative
    // analysis (the expensive part) isn't needed to know that.
    private static bool ReturnsMaybeNull(ExpressionSyntax operand, SemanticModel model, CancellationToken cancellationToken) =>
        operand is InvocationExpressionSyntax
            && model.GetSymbolInfo(operand, cancellationToken).Symbol is IMethodSymbol { ReturnNullableAnnotation: NullableAnnotation.Annotated } method
            && method.GetReturnTypeAttributes().Length == 0;

    // Sonar's S8969 'oblivious' check, exactly: the operand's symbol has no annotation of its own; a 'var' local is judged by its
    // initializer (explicitly typed locals and parameters aren't checked).
    private static bool IsUnannotated(ExpressionSyntax operand, SemanticModel model, CancellationToken cancellationToken) =>
        model.GetSymbolInfo(operand, cancellationToken).Symbol switch
        {
            IPropertySymbol property => property.NullableAnnotation == NullableAnnotation.None,
            IFieldSymbol field => field.NullableAnnotation == NullableAnnotation.None,
            IMethodSymbol method => method.ReturnNullableAnnotation == NullableAnnotation.None,
            ILocalSymbol local => local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken)
                is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true }, Initializer.Value: { } initializer }
                && IsUnannotated(initializer, model, cancellationToken),
            _ => false,
        };

    private static NullableFlowState StateWithout(PostfixUnaryExpressionSyntax node, SemanticModel model, CancellationToken cancellationToken)
    {
        var annotation = new SyntaxAnnotation();
        var operand = node.Operand.WithAdditionalAnnotations(annotation);
        foreach (var ancestor in node.Ancestors())
        {
            SemanticModel? speculative = null;
            SyntaxNode? container = null;
            switch (ancestor)
            {
                case StatementSyntax statement when statement is not BlockSyntax:
                    container = statement.ReplaceNode(node, operand);
                    model.TryGetSpeculativeSemanticModel(statement.SpanStart, (StatementSyntax)container, out speculative);
                    break;

                case EqualsValueClauseSyntax clause when clause.Parent is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax }:
                    container = clause.ReplaceNode(node, operand);
                    model.TryGetSpeculativeSemanticModel(clause.SpanStart, (EqualsValueClauseSyntax)container, out speculative);
                    break;

                case ArrowExpressionClauseSyntax arrow:
                    container = arrow.ReplaceNode(node, operand);
                    model.TryGetSpeculativeSemanticModel(arrow.SpanStart, (ArrowExpressionClauseSyntax)container, out speculative);
                    break;

                default:
                    continue;
            }

            return speculative is not null && container?.GetAnnotatedNodes(annotation).FirstOrDefault() is ExpressionSyntax bound
                ? speculative.GetTypeInfo(bound, cancellationToken).Nullability.FlowState
                : NullableFlowState.None;
        }

        return NullableFlowState.None;
    }

    // A type whose nullability is its top level only: no type parameter, no type arguments (except plain value types, as
    // in 'int?' or '(int, int)'), no array or pointer.
    private static bool IsFlat(ITypeSymbol? type) => type switch
    {
        INamedTypeSymbol named => (named.ContainingType is null || IsFlat(named.ContainingType))
            && named.TypeArguments.All(a => a.IsValueType && IsFlat(a)),
        _ => false,
    };
}
