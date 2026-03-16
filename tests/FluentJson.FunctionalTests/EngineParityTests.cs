using System;
using System.Reflection;
using System.Text.Json;

using FluentAssertions;

using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;

using Newtonsoft.Json.Linq;

using Xunit;

namespace FluentJson.FunctionalTests;

public class EngineParityTests
{
    [Fact]
    public void EngineParity_Should_ProduceAndConsumeIdenticalJson()
    {
        // 1. Arrange & Configure
        // Build the agnostic model by scanning this assembly for OrderConfiguration and OrderLineConfiguration
        var model = new JsonModelBuilder()
            .ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())
            .Build();

        // System.Text.Json Configuration
        var stjOptions = new JsonSerializerOptions().AddFluentJson(model);

        // Newtonsoft.Json Configuration
        var nwSettings = new global::Newtonsoft.Json.JsonSerializerSettings().AddFluentJson(model);

        // 2. Create complex DDD object
        var order = new Order("ORD-2026-991");
        order.AddLine(Guid.NewGuid(), 5);
        order.AddLine(Guid.NewGuid(), 2);

        // Serialize using both engines
        // STJ handles private setters internally with JsonInclude, but we bypassed it with our factory
        var stjJson = System.Text.Json.JsonSerializer.Serialize(order, stjOptions);
        var nwJson = global::Newtonsoft.Json.JsonConvert.SerializeObject(order, nwSettings);

        // 4. Assert: Semantic JSON equivalence
        // Parse to JToken to compare structure and values irrespective of formatting or property order
        var stjToken = JToken.Parse(stjJson);
        var nwToken = JToken.Parse(nwJson);

        // Print for debugging if they don't match
        JToken.DeepEquals(stjToken, nwToken).Should().BeTrue("System.Text.Json and Newtonsoft.Json should produce semantically identical output based on the same FluentJson model.");

        // Assert specific configuration behaviors directly on the token
        stjToken["order_number"].Should().NotBeNull();
        stjToken["CalculatedTotal"].Should().BeNull("CalculatedTotal should be ignored");
        stjToken["Lines"].Should().BeNull();
        stjToken["lines"].Should().NotBeNull("Private backing field _lines should be mapped to 'lines'");
        stjToken["lines"]!.Type.Should().Be(JTokenType.Array);
        stjToken["lines"]!.Children().Should().HaveCount(2);

        // Deserialize using both engines
        var deserializedFromStj = System.Text.Json.JsonSerializer.Deserialize<Order>(stjJson, stjOptions);
        var deserializedFromNw = global::Newtonsoft.Json.JsonConvert.DeserializeObject<Order>(nwJson, nwSettings);

        // 6. Assert: Identical Domain State (Invariant safety)
        deserializedFromStj.Should().NotBeNull();
        deserializedFromNw.Should().NotBeNull();

        // Both engines successfully instantiated the object using the private parameterless constructor
        deserializedFromStj!.OrderNumber.Should().Be("ORD-2026-991");
        deserializedFromNw!.OrderNumber.Should().Be("ORD-2026-991");

        // Both engines successfully bypassed getter-only constraints and populated the private backing field
        deserializedFromStj.Lines.Should().HaveCount(2);
        deserializedFromNw.Lines.Should().HaveCount(2);

        // Both engines respected the ignorance of CalculatedTotal (the property reconstructs its state functionally based on the lines)
        deserializedFromStj.CalculatedTotal.Should().Be(70); // 5*10 + 2*10
        deserializedFromNw.CalculatedTotal.Should().Be(70); // 5*10 + 2*10
    }
}
