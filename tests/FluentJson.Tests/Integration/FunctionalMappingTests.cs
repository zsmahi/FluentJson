using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class FunctionalMappingTests
{
    // --- Domain Model (DDD Style) ---
    public class Product
    {
        // Private backing field (standard DDD encapsulation)
        private string _sku;

        public Product(string sku, string name, decimal price)
        {
            _sku = sku;
            Name = name;
            Price = price;
            InternalSecret = "Hidden";
        }

        // Default constructor for serializers
        private Product() { _sku = ""; Name = ""; InternalSecret = ""; }

        // FIX: Utiliser une propriété Read-Only (C# Standard) au lieu d'une méthode GetSku()
        // Cela permet aux Expression Trees (p => p.Sku) de fonctionner.
        public string Sku => _sku;

        public string Name { get; set; }

        public decimal Price { get; set; }

        public string InternalSecret { get; set; }
    }

    // --- Configuration Class ---
    public class ProductConfiguration : IJsonEntityTypeConfiguration<Product>
    {
        public void Configure(JsonEntityTypeBuilder<Product> builder)
        {
            // 1. Backing Field: On cible la propriété publique 'Sku', 
            // mais on force la sérialisation via le champ privé '_sku'.
            builder.Property(p => p.Sku).HasField("_sku").HasPropertyName("id");

            // 2. Renaming
            builder.Property(p => p.Name).HasPropertyName("product_name");

            // 3. Lambda Conversion
            builder.Property(p => p.Price).HasConversion(
                p => (int)(p * 100), // Write
                j => (decimal)j / 100 // Read
            );

            // 4. Ignore
            builder.Ignore(p => p.InternalSecret);
        }
    }

    [Fact]
    public void Newtonsoft_Should_Handle_Complex_Mapping_And_DDD()
    {
        // Arrange
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ProductConfiguration());
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build();

        var product = new Product("SKU-123", "Laptop", 99.99m);

        // Act
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(product, settings);

        // Assert
        json.Should().Contain("\"id\":\"SKU-123\"");
        json.Should().Contain("\"product_name\":\"Laptop\"");
        json.Should().Contain("9999");
        json.Should().NotContain("Hidden");

        // Round Trip
        Product? restored = NewtonsoftSerial.JsonConvert.DeserializeObject<Product>(json, settings);
        restored.Should().NotBeNull();
        restored!.Sku.Should().Be("SKU-123");
    }

    [Fact]
    public void SystemTextJson_Should_Handle_Complex_Mapping_And_DDD()
    {
        // Arrange
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ProductConfiguration());
        JsonSerializerOptions options = builder.Build();

        var product = new Product("SKU-123", "Laptop", 99.99m);

        // Act
        string json = JsonSerializer.Serialize(product, options);

        // Assert
        json.Should().Contain("\"id\":\"SKU-123\"");
        json.Should().Contain("\"product_name\":\"Laptop\"");
        json.Should().Contain("9999");
        json.Should().NotContain("Hidden");

        // Round Trip
        Product? restored = JsonSerializer.Deserialize<Product>(json, options);
        restored.Should().NotBeNull();
        restored!.Sku.Should().Be("SKU-123");
    }
}
