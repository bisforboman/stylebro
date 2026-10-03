// <copyright file="Order.cs" company="Contoso">
// Copyright (c) Contoso. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Orders;

/// <summary>
/// A customer's order.
/// </summary>
public class Order
{
    private readonly List<string> _items = new List<string>();
    private OrderStatus _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    /// <param name="id">The order number.</param>
    /// <param name="created">When the order was placed.</param>
    public Order(int id, DateTime created)
    {
        Id = id;
        Created = created;
    }

    /// <summary>
    /// Gets the order number.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets when the order was placed.
    /// </summary>
    public DateTime Created { get; }

    /// <summary>
    /// Gets the order's status.
    /// </summary>
    public OrderStatus Status => _status;

    /// <summary>
    /// Gets the items in the order.
    /// </summary>
    public IReadOnlyList<string> Items => _items;

    /// <summary>
    /// Adds an item to an open order.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void AddItem(string item)
    {
        if (_status != OrderStatus.Open)
        {
            throw new InvalidOperationException("Only open orders can change.");
        }

        _items.Add(item);
    }

    /// <summary>
    /// Moves the order to its next status.
    /// </summary>
    public void Advance()
    {
        _status = _status switch
        {
            OrderStatus.Open => OrderStatus.Paid,
            OrderStatus.Paid => OrderStatus.Shipped,
            _ => throw new InvalidOperationException("The order has already shipped.")
        };
    }
}
