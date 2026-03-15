using System;
using System.Collections.Generic;

namespace FluentJson.FunctionalTests;

public class Order
{
    private readonly List<OrderLine> _lines;

    public string OrderNumber { get; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    
    // Ignored in JSON
    public decimal CalculatedTotal
    {
        get
        {
            decimal total = 0;
            foreach (var line in _lines)
            {
                total += line.Quantity * 10; // dummy price
            }
            return total;
        }
    }

    private Order()
    {
        OrderNumber = string.Empty;
        _lines = new List<OrderLine>();
    }

    public Order(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number cannot be empty.", nameof(orderNumber));
            
        OrderNumber = orderNumber;
        _lines = new List<OrderLine>();
    }

    public void AddLine(Guid productId, int quantity)
    {
        _lines.Add(new OrderLine(productId, quantity));
    }
}

public class OrderLine
{
    public Guid ProductId { get; }
    public int Quantity { get; }

    private OrderLine()
    {
        ProductId = Guid.Empty;
        Quantity = 0;
    }

    internal OrderLine(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}
