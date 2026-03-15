using System;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.Sample.LegacyMonolith;

// 1. A typical monolithic entity with strict requirements
public class LegacyCustomer
{
    private LegacyCustomer() { CustomerCode = null!; }

    public LegacyCustomer(long id, string customerCode)
    {
        Id = id;
        CustomerCode = customerCode;
    }

    public long Id { get; }
    public string CustomerCode { get; }
    
    // Some property that isn't mapped properly by default but needs to be included
    public string GlobalRegion { get; set; } = "EMEA";
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== FluentJson Legacy Monolith Integration ===");
        Console.WriteLine("Simulating a .NET 4.8 Framework environment utilizing Newtonsoft.Json\n");

        var builder = new JsonModelBuilder();
        builder.Entity<LegacyCustomer>().Property(x => x.Id).HasName("legacy_id"); // Fluent explicit mapping
        builder.Entity<LegacyCustomer>().Property(x => x.GlobalRegion).HasName("region"); // Overriding the camelCase default

        var model = builder.Build();

        // Simulate an existing monolith's global setup
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            // Existing legacy code relies on this camel case resolver for hundreds of other classes
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // We seamlessly inject our FluentJson configuration into the existing pipeline
        settings.AddFluentJson(model);

        var customer = new LegacyCustomer(99120, "CUST-A-ZZZ")
        {
            GlobalRegion = "NA"
        };

        string json = JsonConvert.SerializeObject(customer, settings);
        Console.WriteLine("Generated JSON payload (blends explicit mappings with fallback camelCase):");
        Console.WriteLine(json);

        var restored = JsonConvert.DeserializeObject<LegacyCustomer>(json, settings)!;
        Console.WriteLine($"\nRestored CustomerCode via compiled constructor factory: {restored.CustomerCode}");
    }
}
