using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Ordering;

/// <summary>Member kinds. Declaration order is the default order (StyleCop SA1201-compatible).</summary>
internal enum MemberKind
{
    Field,
    Constructor,
    Finalizer,
    Delegate,
    Event,
    Enum,
    Interface,
    Property,
    Indexer,
    Conversion,
    Operator,
    Method,
    Struct,
    Class,

    /// <summary>A namespace inside a namespace or file: always first there, like StyleCop (not configurable).</summary>
    Namespace,
}

/// <summary>Accessibility. Declaration order is the default order (StyleCop SA1202-compatible).</summary>
internal enum MemberAccess
{
    Public,
    Internal,
    ProtectedInternal,
    Protected,
    PrivateProtected,
    Private,
}

/// <summary>
/// Ordering options read from .editorconfig / .globalconfig:
/// <code>
/// stylebro_member_order            = field, constructor, finalizer, delegate, event, enum, interface, property, indexer, conversion, operator, method, struct, class
/// stylebro_member_access_order     = public, internal, protected_internal, protected, private_protected, private
/// stylebro_member_constants_first  = true
/// stylebro_member_static_first     = true
/// stylebro_member_readonly_first   = true
/// </code>
/// Kinds or accessibilities left out of a configured list keep their default relative order, after the listed ones.
/// </summary>
internal sealed class MemberOrderOptions
{
    public const string KindOrderKey = "stylebro_member_order";
    public const string AccessOrderKey = "stylebro_member_access_order";
    public const string ConstantsFirstKey = "stylebro_member_constants_first";
    public const string StaticFirstKey = "stylebro_member_static_first";
    public const string ReadonlyFirstKey = "stylebro_member_readonly_first";

    private const int KindCount = (int)MemberKind.Namespace + 1;
    private const int AccessCount = (int)MemberAccess.Private + 1;

    private static readonly Dictionary<string, int> KindNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["field"] = (int)MemberKind.Field,
        ["constructor"] = (int)MemberKind.Constructor,
        ["finalizer"] = (int)MemberKind.Finalizer,
        ["delegate"] = (int)MemberKind.Delegate,
        ["event"] = (int)MemberKind.Event,
        ["enum"] = (int)MemberKind.Enum,
        ["interface"] = (int)MemberKind.Interface,
        ["property"] = (int)MemberKind.Property,
        ["indexer"] = (int)MemberKind.Indexer,
        ["conversion"] = (int)MemberKind.Conversion,
        ["operator"] = (int)MemberKind.Operator,
        ["method"] = (int)MemberKind.Method,
        ["struct"] = (int)MemberKind.Struct,
        ["class"] = (int)MemberKind.Class,
    };

    private static readonly Dictionary<string, int> AccessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["public"] = (int)MemberAccess.Public,
        ["internal"] = (int)MemberAccess.Internal,
        ["protected_internal"] = (int)MemberAccess.ProtectedInternal,
        ["protected"] = (int)MemberAccess.Protected,
        ["private_protected"] = (int)MemberAccess.PrivateProtected,
        ["private"] = (int)MemberAccess.Private,
    };

    public static readonly MemberOrderOptions Default = new(null, null, true, true, true);

    private readonly int[] kindRanks;
    private readonly int[] accessRanks;

    private MemberOrderOptions(
        List<int>? kindOrder,
        List<int>? accessOrder,
        bool constantsFirst,
        bool staticFirst,
        bool readonlyFirst)
    {
        kindRanks = BuildRanks(kindOrder, KindCount);
        accessRanks = BuildRanks(accessOrder, AccessCount);
        ConstantsFirst = constantsFirst;
        StaticFirst = staticFirst;
        ReadonlyFirst = readonlyFirst;
    }

    public bool ConstantsFirst { get; }

    public bool StaticFirst { get; }

    public bool ReadonlyFirst { get; }

    public int KindRank(MemberKind kind) => kindRanks[(int)kind];

    public int AccessRank(MemberAccess access) => accessRanks[(int)access];

    public static MemberOrderOptions Read(AnalyzerConfigOptions options)
    {
        var kindOrder = ParseList(options, KindOrderKey, KindNames);
        var accessOrder = ParseList(options, AccessOrderKey, AccessNames);
        var constantsFirst = ReadBool(options, ConstantsFirstKey, true);
        var staticFirst = ReadBool(options, StaticFirstKey, true);
        var readonlyFirst = ReadBool(options, ReadonlyFirstKey, true);

        if (kindOrder is null && accessOrder is null && constantsFirst && staticFirst && readonlyFirst)
        {
            return Default;
        }

        return new MemberOrderOptions(kindOrder, accessOrder, constantsFirst, staticFirst, readonlyFirst);
    }

    /// <summary>Configured entries rank first (in configured order); the rest follow in default order.</summary>
    private static int[] BuildRanks(List<int>? configured, int count)
    {
        var ranks = new int[count];
        for (var i = 0; i < count; i++)
        {
            ranks[i] = -1;
        }

        var next = 0;
        if (configured is not null)
        {
            foreach (var value in configured)
            {
                if (ranks[value] == -1)
                {
                    ranks[value] = next++;
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            if (ranks[i] == -1)
            {
                ranks[i] = next++;
            }
        }

        return ranks;
    }

    private static List<int>? ParseList(AnalyzerConfigOptions options, string key, Dictionary<string, int> names)
    {
        if (!options.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var result = new List<int>();
        foreach (var part in value.Split(','))
        {
            if (names.TryGetValue(part.Trim(), out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result.Count == 0 ? null : result;
    }

    private static bool ReadBool(AnalyzerConfigOptions options, string key, bool defaultValue)
    {
        return options.TryGetValue(key, out var value)
            && value is not null
            && bool.TryParse(value.Trim(), out var parsed)
                ? parsed
                : defaultValue;
    }
}
