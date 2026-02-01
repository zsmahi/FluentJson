using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Collections.Concurrent;

namespace FluentJson.Tests.Integration;

public class ConcurrencyTests
{
    public class ConcurrentEntity
    {
        public string Data { get; set; } = "";
    }

    public class ConcurrentConfig : IJsonEntityTypeConfiguration<ConcurrentEntity>
    {
        public void Configure(JsonEntityTypeBuilder<ConcurrentEntity> builder)
        {
            builder.Property(x => x.Data).HasPropertyName("data");
        }
    }

    [Fact]
    public async Task Builder_Should_Handle_Concurrent_Configuration_Requests()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        int iterations = 100;
        var exceptions = new ConcurrentBag<Exception>();

        await Task.WhenAll(Enumerable.Range(0, iterations).Select(_ => Task.Run(() =>
        {
            try
            {
                builder.ApplyConfiguration(new ConcurrentConfig());
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })));

        exceptions.Should().BeEmpty();
        System.Text.Json.JsonSerializerOptions options = builder.Build();
        options.Should().NotBeNull();
    }
}
