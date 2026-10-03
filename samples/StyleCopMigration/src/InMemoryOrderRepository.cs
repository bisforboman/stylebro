// <copyright file="InMemoryOrderRepository.cs" company="Contoso">
// Copyright (c) Contoso. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Orders;

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = new List<Order>();

    public void Add(Order order)
    {
        _orders.Add(order);
    }

    public IReadOnlyList<Order> FindByStatus(OrderStatus status)
    {
        return _orders
            .Where(o => o.Status == status)
            .OrderBy(o => o.Created)
            .ToList();
    }
}
