using FluentAssertions;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class ResilienceTests
{
    public class ComplexTypeEntity
    {
        public (int Id, string Name) TupleProp { get; set; }
        public static string StaticProp { get; set; } = "IgnoreMe";
        public IntPtr NativePtr { get; set; } // Edge case for reflection
    }

    [Fact]
    public void Should_Gracefully_Handle_Complex_And_Static_Members()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        JsonSerializerOptions options = builder.Build();

        var entity = new ComplexTypeEntity { TupleProp = (1, "Test") };
        string json = JsonSerializer.Serialize(entity, options);

        // Verification: Static properties must be ignored by default
        json.Should().NotContain("IgnoreMe");
        json.Should().Contain("TupleProp");
    }
}
