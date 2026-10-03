// <copyright file="IOrderRepository.cs" company="Contoso">
// Copyright (c) Contoso. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Orders;

/// <summary>
/// Stores orders.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds an order.
    /// </summary>
    /// <param name="order">The order to add.</param>
    void Add(Order order);

    /// <summary>
    /// Finds the orders with the given status.
    /// </summary>
    /// <param name="status">The status to look for.</param>
    /// <returns>The matching orders, oldest first.</returns>
    IReadOnlyList<Order> FindByStatus(OrderStatus status);
}
