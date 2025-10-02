using System.Text.Json.Serialization;

namespace Kafka.Producer.Models;

public class OrderSubmittedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public ShippingAddress ShippingAddress { get; set; } = new();
    public PaymentInfo Payment { get; set; } = new();
    public required string Status { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventVersion { get; } = "1.0";
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ShippingAddress
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class PaymentInfo
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}