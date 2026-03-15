using System;
using System.Text.Json;
using FluentJson.Core.Builder;
using FluentJson.SystemTextJson;

namespace FluentJson.Sample.QuickStart;

// 1. A traditional POCO with no attributes
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== FluentJson QuickStart ===");

        // 2. Define your metadata mapping entirely decoupled from the entity
        var builder = new JsonModelBuilder();
        builder.Entity<User>().Property(u => u.Id).HasName("user_id").IsRequired();
        builder.Entity<User>().Property(u => u.Name).HasName("full_name");
        builder.Entity<User>().Property(u => u.Email).HasName("contact_email");

        var model = builder.Build();

        // 3. Register the model with System.Text.Json
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.AddFluentJson(model);

        // 4. Serialize!
        var user = new User { Id = Guid.NewGuid(), Name = "Grace Hopper", Email = "grace@example.com" };
        string json = JsonSerializer.Serialize(user, options);
        
        Console.WriteLine("Serialized JSON:");
        Console.WriteLine(json);

        // 5. Deserialize!
        var deserializedUser = JsonSerializer.Deserialize<User>(json, options)!;
        Console.WriteLine($"\nDeserialized User: {deserializedUser.Name} ({deserializedUser.Email})");
    }
}
