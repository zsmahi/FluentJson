using FluentAssertions;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class LargePayloadTests
{
    public class LargeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    [Fact]
    public void Should_Process_Large_Collections_Without_Performance_Degradation()
    {
        // Arrange
        var builder = new FluentJson.SystemTextJson.JsonModelBuilder();
        var options = builder.Build();

        
        var data = Enumerable.Range(0, 10000)
            .Select(i => new LargeEntity
            {
                Id = i,
                Name = $"Name_{i}",
                Description = "Consistent large payload data for performance benchmarking."
            })
            .ToList();

        // Warm up
        JsonSerializer.Serialize(data.Take(10), options);

        // Act
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string json = JsonSerializer.Serialize(data, options);
        watch.Stop();

        // Assert
        watch.ElapsedMilliseconds.Should().BeLessThan(1000, "Serialization of 10k items must be highly efficient");
        json.Should().NotBeNullOrEmpty();
    }
}
