using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentJson.Core.Builder;
using FluentJson.SystemTextJson;

namespace FluentJson.Sample.DDD;

// 1. The Complex Enterprise Aggregate
public class Order
{
    // Real DDD entities hide their default constructor from the public
    private Order() { OrderNumber = null!; Status = null!; }

    public Order(string orderNumber)
    {
        OrderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
        Status = "Pending";
    }

    // Get-only property (No public setter)
    public string OrderNumber { get; }
    
    // Private property setter (Encapsulated)
    public string Status { get; private set; }

    // Fully encapsulated collection backed by a private field
    private readonly List<string> _items = new();
    public IReadOnlyCollection<string> Items => _items.AsReadOnly();

    public void AddItem(string itemName)
    {
        _items.Add(itemName);
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== FluentJson DDD Invariant Protection ===");

        // 2. Map the encapsulated DDD constraints
        var builder = new JsonModelBuilder();
        builder.Entity<Order>().Property(x => x.OrderNumber).HasName("order_number").IsRequired();
        builder.Entity<Order>().Property(x => x.Status).HasName("current_status");
        // Map the strongly hidden private backing field explicitly
        builder.Entity<Order>().Property<List<string>>("_items").HasName("purchased_items");

        var model = builder.Build();

        // 3. Register the pure domain model with System.Text.Json
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.AddFluentJson(model);

        // 4. Serialize
        var order = new Order("ORD-2026-999");
        order.AddItem("Quantum Keyboard");
        order.AddItem("Neural Mouse");

        string json = JsonSerializer.Serialize(order, options);
        Console.WriteLine("\nSerialized JSON:");
        Console.WriteLine(json);

        // 5. Deserialize! (FluentJson will bypass the private constructor and map the private fields automatically)
        var restoredOrder = JsonSerializer.Deserialize<Order>(json, options)!;
        Console.WriteLine($"\nRestored Order: {restoredOrder.OrderNumber}");
        Console.WriteLine($"Restored Status: {restoredOrder.Status}");
        Console.WriteLine($"Restored Items Count: {restoredOrder.Items.Count}");
        foreach(var item in restoredOrder.Items)
        {
            Console.WriteLine($" - {item}");
        }
    }
}
