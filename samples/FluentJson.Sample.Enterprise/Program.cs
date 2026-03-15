using System;
using System.Reflection;
using System.Text.Json;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;
using Newtonsoft.Json;

namespace FluentJson.Sample.Enterprise;

// 1. The Models
public class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class Company
{
    public string CompanyName { get; set; } = string.Empty;
    public Employee[] Employees { get; set; } = Array.Empty<Employee>();
}

// 2. The Modular Configurations
public class EmployeeConfiguration : IJsonTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(x => x.Id).HasName("emp_id");
        builder.Property(x => x.Name).HasName("full_name");
        builder.Property(x => x.Department).HasName("dept");
    }
}

public class CompanyConfiguration : IJsonTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(x => x.CompanyName).HasName("corp_name");
        builder.Property(x => x.Employees).HasName("staff");
    }
}

// 3. The Execution
public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== FluentJson Enterprise Modular Dual-Engine Configuration ===");

        // Scan current assembly for any class implementing IJsonTypeConfiguration<T>
        var builder = new JsonModelBuilder();
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        var model = builder.Build();

        var company = new Company
        {
            CompanyName = "Acme Corp",
            Employees = new[]
            {
                new Employee { Id = Guid.NewGuid(), Name = "Alice", Department = "Engineering" },
                new Employee { Id = Guid.NewGuid(), Name = "Bob", Department = "Sales" }
            }
        };

        // System.Text.Json Setup
        var stjOptions = new JsonSerializerOptions { WriteIndented = true };
        stjOptions.AddFluentJson(model);
        string stjJson = System.Text.Json.JsonSerializer.Serialize(company, stjOptions);

        // Newtonsoft.Json Setup
        var nwSettings = new global::Newtonsoft.Json.JsonSerializerSettings { Formatting = global::Newtonsoft.Json.Formatting.Indented };
        nwSettings.AddFluentJson(model);
        string nwJson = global::Newtonsoft.Json.JsonConvert.SerializeObject(company, nwSettings);

        Console.WriteLine("\n--- System.Text.Json Output ---");
        Console.WriteLine(stjJson);

        Console.WriteLine("\n--- Newtonsoft.Json Output ---");
        Console.WriteLine(nwJson);

        Console.WriteLine($"\nAre outputs identical? {stjJson == nwJson}");
    }
}
