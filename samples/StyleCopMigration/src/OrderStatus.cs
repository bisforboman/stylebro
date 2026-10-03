// <copyright file="OrderStatus.cs" company="Contoso">
// Copyright (c) Contoso. All rights reserved.
// </copyright>

namespace Orders;

/// <summary>
/// The states an order goes through.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Created, not paid yet.
    /// </summary>
    Open,

    /// <summary>
    /// Paid and waiting to ship.
    /// </summary>
    Paid,

    /// <summary>
    /// On its way to the customer.
    /// </summary>
    Shipped
}
