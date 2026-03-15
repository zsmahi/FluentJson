using System;
using System.Collections.Generic;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;

namespace FluentJson.Sample.CleanArchitecture;

// --- DOMAIN LAYER ---

/// <summary>
/// A Strongly-Typed ID (Value Object)
/// </summary>
public struct UserId
{
    public Guid Value { get; }
    public UserId(Guid value) => Value = value;
}

public class User
{
    // Private state and non-public instantiation
    private readonly List<Role> _roles = new();

    public UserId Id { get; private set; }
    public string Username { get; private set; }
    
    // Circular reference
    public Department Department { get; private set; }
    
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private User() { } // Handled by FluentJson

    public User(UserId id, string username, Department department)
    {
        Id = id;
        Username = username;
        Department = department;
    }

    public void AddRole(Role role) => _roles.Add(role);
}

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    // Circular reference back to User
    public List<User> Users { get; set; } = new();
}

public abstract class Role
{
    public string Title { get; set; } = string.Empty;
}

public class AdminRole : Role
{
    public int ClearanceLevel { get; set; }
}

public class GuestRole : Role
{
    public DateTime ExpirationDate { get; set; }
}

// --- INFRASTRUCTURE LAYER (Configurations) ---

public class UserConfiguration : IJsonTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Id).HasName("id")
               .HasConversion<Guid>(id => id.Value, scalar => new UserId(scalar));
               
        builder.Property(x => x.Username).HasName("userName");
        
        builder.Property(x => x.Department).HasName("department");
        
        builder.Property<List<Role>>("_roles").HasName("roles");
        
        builder.Ignore(x => x.Roles);
        
        builder.PreserveReferences();
    }
}

public class DepartmentConfiguration : IJsonTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(x => x.Id).HasName("id");
        builder.Property(x => x.Name).HasName("name");
        builder.Property(x => x.Users).HasName("users");
        
        builder.PreserveReferences();
    }
}

public class RoleConfiguration : IJsonTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(x => x.Title).HasName("title");
        
        builder.HasDiscriminator("roleType")
               .HasDerivedType<AdminRole>("admin")
               .HasDerivedType<GuestRole>("guest");
    }
}

public class AdminRoleConfiguration : IJsonTypeConfiguration<AdminRole>
{
    public void Configure(EntityTypeBuilder<AdminRole> builder)
    {
        builder.Property(x => x.ClearanceLevel).HasName("clearance");
    }
}

public class GuestRoleConfiguration : IJsonTypeConfiguration<GuestRole>
{
    public void Configure(EntityTypeBuilder<GuestRole> builder)
    {
        builder.Property(x => x.ExpirationDate).HasName("expires");
    }
}

// --- COMPOSITION ROOT ---

public class Program
{
    public static void Main()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" FluentJson: Clean Architecture & OCP Sample");
        Console.WriteLine("==================================================\n");

        // 1. OCP Bootstrapping (Assembly Scanning)
        var builder = new JsonModelBuilder();
        builder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);
        var model = builder.Build();

        // 2. Domain Instantiation
        var engineering = new Department { Name = "Engineering" };
        var alice = new User(new UserId(Guid.NewGuid()), "alice_admin", engineering);
        alice.AddRole(new AdminRole { Title = "System Admin", ClearanceLevel = 5 });
        alice.AddRole(new GuestRole { Title = "External Access", ExpirationDate = DateTime.UtcNow.AddDays(30) });
        engineering.Users.Add(alice);

        // 3. System.Text.Json Serialization
        var stjOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        stjOptions.AddFluentJson(model);

        Console.WriteLine("--- System.Text.Json Output (With Flattened ValueObjects, Polymorphism, and Circular Preservation) ---");
        string stjJson = System.Text.Json.JsonSerializer.Serialize(engineering, stjOptions);
        Console.WriteLine(stjJson);
        Console.WriteLine("\nDeserialization successful? " + (System.Text.Json.JsonSerializer.Deserialize<Department>(stjJson, stjOptions) != null));


        // 4. Newtonsoft.Json Serialization
        var nwSettings = new Newtonsoft.Json.JsonSerializerSettings 
        { 
            Formatting = Newtonsoft.Json.Formatting.Indented,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize
        };
        nwSettings.AddFluentJson(model);

        Console.WriteLine("\n--- Newtonsoft.Json Output ---");
        string nwJson = Newtonsoft.Json.JsonConvert.SerializeObject(engineering, nwSettings);
        Console.WriteLine(nwJson);
        Console.WriteLine("\nDeserialization successful? " + (Newtonsoft.Json.JsonConvert.DeserializeObject<Department>(nwJson, nwSettings) != null));
    }
}
