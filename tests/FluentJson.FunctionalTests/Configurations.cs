using System.Collections.Generic;
using FluentJson.Core.Builder;

namespace FluentJson.FunctionalTests;

public class OrderConfiguration : IJsonTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.OrderNumber).HasName("order_number").IsRequired();
        builder.Ignore(x => x.CalculatedTotal);
        
        // Map the private backing field for Lines
        builder.Property<List<OrderLine>>("_lines").HasName("lines");
    }
}

public class OrderLineConfiguration : IJsonTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.Property(x => x.ProductId).HasName("product_id").IsRequired();
        builder.Property(x => x.Quantity).HasName("quantity").IsRequired();
    }
}
