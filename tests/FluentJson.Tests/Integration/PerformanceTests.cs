using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using FluentJson.SystemTextJson;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FluentJson.Tests.Integration;

public class PerformanceTests
{
    private sealed class LargeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Node
    {
        public string Name { get; set; } = string.Empty;
        public Node? Child { get; set; }
    }

    private sealed class LargeEntityConfiguration : IJsonEntityTypeConfiguration<LargeEntity>
    {
        public void Configure(JsonEntityTypeBuilder<LargeEntity> builder)
        {
            builder.Property(x => x.Name).HasPropertyName("display_name");
        }
    }

    private sealed class NodeConfiguration : IJsonEntityTypeConfiguration<Node>
    {
        public void Configure(JsonEntityTypeBuilder<Node> builder)
        {
            builder.Property(x => x.Name).HasPropertyName("node_name");
        }
    }

    [Fact]
    public void ColdStart_InitialConfiguration_ShouldBeReasonable()
    {
        var timer = Stopwatch.StartNew();

        var builder = new JsonModelBuilder();
        builder.ApplyConfiguration(new LargeEntityConfiguration());

        var options = builder.Build();

        var payload = new LargeEntity { Name = "Bench" };
        _ = System.Text.Json.JsonSerializer.Serialize(payload, options);

        timer.Stop();

        timer.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public void MassiveConcurrency_ShouldBeThreadSafe()
    {
        var builder = new JsonModelBuilder();
        var options = builder.Build();
        var syncFailures = new ConcurrentBag<Exception>();

        // Stress test 
        Parallel.For(0, 100, i => {
            try
            {
                var instance = new { Index = i, Timestamp = DateTime.UtcNow };
                _ = System.Text.Json.JsonSerializer.Serialize(instance, options);
            }
            catch (Exception ex)
            {
                syncFailures.Add(ex);
            }
        });

        syncFailures.Should().BeEmpty();
    }

    [Fact]
    public void DeepGraph_ShouldAvoidStackOverflow_And_StayPerformant()
    {
        var builder = new JsonModelBuilder();
        builder.ApplyConfiguration(new NodeConfiguration());
        var options = builder.Build();

        // deepest hierarchy (50 level)
        var root = new Node { Name = "Root" };
        var current = root;
        for (int i = 1; i <= 50; i++)
        {
            current.Child = new Node { Name = $"L_{i}" };
            current = current.Child;
        }

        var timer = Stopwatch.StartNew();
        var json = System.Text.Json.JsonSerializer.Serialize(root, options);
        timer.Stop();

        json.Should().Contain("L_50");
        timer.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
