using System;
using System.Collections.Generic;

using FluentAssertions;

using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;

using Newtonsoft.Json;

using Xunit;

namespace FluentJson.FunctionalTests;

public class CircularReferenceTests
{
    public class Department
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<Employee> Employees { get; set; } = new();
    }

    public class Employee
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Department? Department { get; set; }
    }

    [Fact]
    public void DualEngine_Should_PreserveReferences_And_Prevent_StackOverflow()
    {
        // 1. Arrange Model with cycles
        var builder = new JsonModelBuilder();
        builder.Entity<Department>().Property(x => x.Id).HasName("id");
        builder.Entity<Department>().Property(x => x.Name).HasName("name");
        builder.Entity<Department>().Property(x => x.Employees).HasName("employees");
        builder.Entity<Department>().PreserveReferences();

        builder.Entity<Employee>().Property(x => x.Id).HasName("id");
        builder.Entity<Employee>().Property(x => x.Name).HasName("name");
        builder.Entity<Employee>().Property(x => x.Department).HasName("department");
        builder.Entity<Employee>().PreserveReferences();

        var model = builder.Build();

        // 2. Arrange Data
        var dept = new Department { Name = "Engineering" };
        var emp1 = new Employee { Name = "Alice", Department = dept };
        var emp2 = new Employee { Name = "Bob", Department = dept };
        dept.Employees.Add(emp1);
        dept.Employees.Add(emp2);

        // 3. System.Text.Json Serialization & Deserialization
        var stjOptions = new System.Text.Json.JsonSerializerOptions();
        stjOptions.AddFluentJson(model);

        string stjJson = System.Text.Json.JsonSerializer.Serialize(dept, stjOptions);
        var stjResult = System.Text.Json.JsonSerializer.Deserialize<Department>(stjJson, stjOptions);

        // 4. Newtonsoft.Json Serialization & Deserialization
        var nwSettings = new JsonSerializerSettings
        {
            // Note: For Newtonsoft, turning on IsReference in contract resolver requires ReferenceLoopHandling to be set globally or it will throw on serialize
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };
        nwSettings.AddFluentJson(model);

        string nwJson = JsonConvert.SerializeObject(dept, nwSettings);
        var nwResult = JsonConvert.DeserializeObject<Department>(nwJson, nwSettings);

        // 5. Assert System.Text.Json Correctness
        stjResult.Should().NotBeNull();
        stjResult!.Name.Should().Be("Engineering");
        stjResult.Employees.Should().HaveCount(2);
        stjResult.Employees[0].Name.Should().Be("Alice");
        stjResult.Employees[0].Department.Should().BeSameAs(stjResult); // Verify reference is circular

        // 6. Assert Newtonsoft.Json Correctness
        nwResult.Should().NotBeNull();
        nwResult!.Name.Should().Be("Engineering");
        nwResult.Employees.Should().HaveCount(2);
        nwResult.Employees[0].Name.Should().Be("Alice");
        nwResult.Employees[0].Department.Should().BeSameAs(nwResult); // Verify reference is circular
    }
}
