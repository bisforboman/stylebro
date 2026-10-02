using System;

namespace StyleBro.Analyzers.Ordering;

internal enum OrderingComponent
{
    None,
    Kind,
    Access,
    Constant,
    Static,
    Readonly,
}

/// <summary>Sort key for a member: kind, then accessibility, then const, static and readonly.</summary>
internal readonly struct MemberKey : IComparable<MemberKey>
{
    public MemberKey(MemberKind kind, MemberAccess access, int kindRank, int accessRank, int constantRank, int staticRank, int readonlyRank)
    {
        Kind = kind;
        Access = access;
        KindRank = kindRank;
        AccessRank = accessRank;
        ConstantRank = constantRank;
        StaticRank = staticRank;
        ReadonlyRank = readonlyRank;
    }

    public MemberKind Kind { get; }

    public MemberAccess Access { get; }

    public int KindRank { get; }

    public int AccessRank { get; }

    public int ConstantRank { get; }

    public int StaticRank { get; }

    public int ReadonlyRank { get; }

    /// <inheritdoc/>
    public int CompareTo(MemberKey other)
    {
        var c = KindRank.CompareTo(other.KindRank);
        if (c != 0)
        {
            return c;
        }

        c = AccessRank.CompareTo(other.AccessRank);
        if (c != 0)
        {
            return c;
        }

        c = ConstantRank.CompareTo(other.ConstantRank);
        if (c != 0)
        {
            return c;
        }

        c = StaticRank.CompareTo(other.StaticRank);
        return c != 0 ? c : ReadonlyRank.CompareTo(other.ReadonlyRank);
    }

    public OrderingComponent FirstDifference(MemberKey other)
    {
        if (KindRank != other.KindRank)
        {
            return OrderingComponent.Kind;
        }

        if (AccessRank != other.AccessRank)
        {
            return OrderingComponent.Access;
        }

        if (ConstantRank != other.ConstantRank)
        {
            return OrderingComponent.Constant;
        }

        if (StaticRank != other.StaticRank)
        {
            return OrderingComponent.Static;
        }

        return ReadonlyRank != other.ReadonlyRank ? OrderingComponent.Readonly : OrderingComponent.None;
    }
}
