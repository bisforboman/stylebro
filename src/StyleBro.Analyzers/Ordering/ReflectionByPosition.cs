using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// Code that reads a type's members by position through reflection: <c>typeof(T).GetFields(flags)[0]</c>,
/// <c>x.GetType().GetProperties().First()</c>, or the array kept in a local and indexed later. Reflection returns members in
/// declaration order, so BRO1001's sort would change what such code reads (Stateless's tests took an event's backing field
/// as <c>GetFields(...)[0]</c> and a list as <c>[1]</c>: 5 tests failed). Syntax only, so it also works on the files of
/// projects a run doesn't load (<see cref="RepositoryNames"/>): the type is known by its simple name, from <see langword="typeof"/>,
/// <c>new T()</c>, <see langword="this"/>, or the declared type of the field, property, local or parameter whose GetType() is called.
/// </summary>
public static class ReflectionByPosition
{
    /// <summary>
    /// The simple name of the type whose members <paramref name="token"/> (the name of a GetFields, GetProperties,
    /// GetMembers or GetEvents call) reads by position, or null.
    /// </summary>
    public static string? GetReadType(SyntaxToken token)
    {
        if (token.ValueText is not ("GetFields" or "GetProperties" or "GetMembers" or "GetEvents")
            || token.Parent is not IdentifierNameSyntax { Parent: MemberAccessExpressionSyntax access } name
            || access.Name != name
            || access.Parent is not InvocationExpressionSyntax invocation
            || !IsReadByPosition(invocation))
        {
            return null;
        }

        return access.Expression switch
        {
            TypeOfExpressionSyntax type => RightmostName(type.Type),
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetType" } getType, ArgumentList.Arguments.Count: 0 } => TypeOf(getType.Expression),
            _ => null,
        };
    }

    /// <summary>Whether a read through <paramref name="method"/> sees every member's position (GetMembers), not only data members'.</summary>
    public static bool ReadsEveryMember(string method) => method == "GetMembers";

    private static bool IsReadByPosition(ExpressionSyntax result)
    {
        if (IsIndexed(result))
        {
            return true;
        }

        // 'var fields = ...GetFields(); ... fields[0]': the local, indexed in the block around it.
        if (result.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable }
            && variable.FirstAncestorOrSelf<BlockSyntax>() is { } block)
        {
            var local = variable.Identifier.ValueText;
            return block.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.ValueText == local && IsIndexed(n));
        }

        return false;
    }

    // 'x[0]', 'x.First()', 'x.ElementAt(1)', 'x.Last()'.
    private static bool IsIndexed(ExpressionSyntax expression) =>
        (expression.Parent is ElementAccessExpressionSyntax element && element.Expression == expression)
        || (expression.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "First" or "FirstOrDefault" or "Last" or "LastOrDefault" or "ElementAt" or "ElementAtOrDefault" } member
            && member.Expression == expression
            && member.Parent is InvocationExpressionSyntax);

    private static string? TypeOf(ExpressionSyntax expression) => expression switch
    {
        ObjectCreationExpressionSyntax creation => RightmostName(creation.Type),
        ThisExpressionSyntax => expression.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>()?.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member => DeclaredType(expression, member.Name.Identifier.ValueText),
        IdentifierNameSyntax identifier => DeclaredType(expression, identifier.Identifier.ValueText),
        _ => null,
    };

    // The explicit type of the field, property, local or parameter with that name in the file ('var' gives nothing).
    private static string? DeclaredType(SyntaxNode at, string name)
    {
        foreach (var node in at.SyntaxTree.GetRoot().DescendantNodes())
        {
            var type = node switch
            {
                VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } declarator when declarator.Identifier.ValueText == name => declaration.Type,
                PropertyDeclarationSyntax property when property.Identifier.ValueText == name => property.Type,
                ParameterSyntax parameter when parameter.Identifier.ValueText == name => parameter.Type,
                _ => null,
            };
            if (type is not null and not IdentifierNameSyntax { IsVar: true })
            {
                return RightmostName(type);
            }
        }

        return null;
    }

    private static string? RightmostName(TypeSyntax type) => type switch
    {
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        _ => null,
    };
}
