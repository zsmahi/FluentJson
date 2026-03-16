using System;
using System.Collections.Generic;

using FluentAssertions;

using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;

using Newtonsoft.Json;

using Xunit;

namespace FluentJson.FunctionalTests;

public class PolymorphismTests
{
    // The Domain Hierarchy
    public abstract class Payment
    {
        public Guid Id { get; protected set; }
        public decimal Amount { get; protected set; }

        // Private parameterless constructor for DDD framework binding
        protected Payment() { }

        protected Payment(decimal amount)
        {
            Id = Guid.NewGuid();
            Amount = amount;
        }
    }

    public class CreditCardPayment : Payment
    {
        private CreditCardPayment() { CardNumber = string.Empty; }

        public CreditCardPayment(decimal amount, string cardNumber) : base(amount)
        {
            CardNumber = cardNumber ?? throw new ArgumentNullException(nameof(cardNumber));
        }

        public string CardNumber { get; private set; }
    }

    public class PaypalPayment : Payment
    {
        private PaypalPayment() { EmailAddress = string.Empty; }

        public PaypalPayment(decimal amount, string email) : base(amount)
        {
            EmailAddress = email ?? throw new ArgumentNullException(nameof(email));
        }

        public string EmailAddress { get; private set; }
    }

    // A Root wrapper
    public class Order
    {
        public List<Payment> Payments { get; set; } = new();
    }

    [Fact]
    public void DualEngine_Should_Deserialize_PolymorphicHierarchy_Correctly()
    {
        // 1. Configure the immutable model
        var builder = new JsonModelBuilder();

        builder.Entity<Order>().Property(x => x.Payments).HasName("transactions");

        builder.Entity<Payment>().Property(x => x.Id).HasName("id");
        builder.Entity<Payment>().Property(x => x.Amount).HasName("amt");

        builder.Entity<Payment>()
            .HasDiscriminator("type")
            .HasDerivedType<CreditCardPayment>("cc")
            .HasDerivedType<PaypalPayment>("pp");

        builder.Entity<CreditCardPayment>()
            .Property(x => x.CardNumber).HasName("card_num");

        builder.Entity<PaypalPayment>()
            .Property(x => x.EmailAddress).HasName("email_addr");

        var model = builder.Build();

        // 2. Prepare the JSON Payload
        string payload = @"
        {
            ""transactions"": [
                {
                    ""type"": ""cc"",
                    ""id"": ""00000000-0000-0000-0000-000000000001"",
                    ""amt"": 99.50,
                    ""card_num"": ""4444-5555-6666-7777""
                },
                {
                    ""type"": ""pp"",
                    ""id"": ""00000000-0000-0000-0000-000000000002"",
                    ""amt"": 42.00,
                    ""email_addr"": ""john@doe.com""
                }
            ]
        }";

        // 3. System.Text.Json Deserialization
        var stjOptions = new System.Text.Json.JsonSerializerOptions();
        stjOptions.AddFluentJson(model);
        var stjOrder = System.Text.Json.JsonSerializer.Deserialize<Order>(payload, stjOptions)!;

        // 4. Newtonsoft.Json Deserialization
        var nwSettings = new JsonSerializerSettings();
        nwSettings.AddFluentJson(model);
        var nwOrder = JsonConvert.DeserializeObject<Order>(payload, nwSettings)!;

        // 5. Assert Engine Parity & Correctness
        stjOrder.Payments.Should().HaveCount(2);
        nwOrder.Payments.Should().HaveCount(2);

        // Assert STJ Instances
        stjOrder.Payments[0].Should().BeOfType<CreditCardPayment>();
        ((CreditCardPayment)stjOrder.Payments[0]).CardNumber.Should().Be("4444-5555-6666-7777");
        stjOrder.Payments[0].Amount.Should().Be(99.50m);

        stjOrder.Payments[1].Should().BeOfType<PaypalPayment>();
        ((PaypalPayment)stjOrder.Payments[1]).EmailAddress.Should().Be("john@doe.com");
        stjOrder.Payments[1].Amount.Should().Be(42.00m);

        // Assert Newtonsoft Instances
        nwOrder.Payments[0].Should().BeOfType<CreditCardPayment>();
        ((CreditCardPayment)nwOrder.Payments[0]).CardNumber.Should().Be("4444-5555-6666-7777");
        nwOrder.Payments[0].Amount.Should().Be(99.50m);

        nwOrder.Payments[1].Should().BeOfType<PaypalPayment>();
        ((PaypalPayment)nwOrder.Payments[1]).EmailAddress.Should().Be("john@doe.com");
        nwOrder.Payments[1].Amount.Should().Be(42.00m);
    }

    [Fact]
    public void MissingDiscriminator_Should_FailFast_InBothEngines()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Payment>()
            .HasDiscriminator("type")
            .HasDerivedType<CreditCardPayment>("cc");
        var model = builder.Build();

        string payload = @"
        {
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""amt"": 99.50
        }";

        // Assert System.Text.Json throws JsonException from the new Custom Converter Factory
        var stjOptions = new System.Text.Json.JsonSerializerOptions();
        stjOptions.AddFluentJson(model);
        Action stjAction = () => System.Text.Json.JsonSerializer.Deserialize<Payment>(payload, stjOptions);
        stjAction.Should().Throw<System.Text.Json.JsonException>();

        // Assert Newtonsoft.Json throws JsonSerializationException
        var nwSettings = new JsonSerializerSettings();
        nwSettings.AddFluentJson(model);
        Action nwAction = () => JsonConvert.DeserializeObject<Payment>(payload, nwSettings);
        nwAction.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void UnknownDiscriminator_Should_FailFast_InBothEngines()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Payment>()
            .HasDiscriminator("type")
            .HasDerivedType<CreditCardPayment>("cc");
        var model = builder.Build();

        string payload = @"
        {
            ""type"": ""bitcoin"",
            ""id"": ""00000000-0000-0000-0000-000000000001"",
            ""amt"": 99.50
        }";

        // Assert System.Text.Json throws
        var stjOptions = new System.Text.Json.JsonSerializerOptions();
        stjOptions.AddFluentJson(model);
        // Note: fail serialization behavior defaults to throwing NotSupportedException when an unknown type is met.
        Action stjAction = () => System.Text.Json.JsonSerializer.Deserialize<Payment>(payload, stjOptions);
        stjAction.Should().Throw<Exception>(); // Catch either JsonException or NotSupportedException

        // Assert Newtonsoft.Json throws
        var nwSettings = new JsonSerializerSettings();
        nwSettings.AddFluentJson(model);
        Action nwAction = () => JsonConvert.DeserializeObject<Payment>(payload, nwSettings);
        nwAction.Should().Throw<JsonSerializationException>();
    }
}
