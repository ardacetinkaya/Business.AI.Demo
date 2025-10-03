namespace Kafka.Consumer.Models;

// DTO to match the producer's OrderSubmittedEvent structure
public class OrderSubmittedEventDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public ShippingAddressDto ShippingAddress { get; set; } = new();
    public PaymentInfoDto Payment { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventVersion { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ShippingAddressDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class PaymentInfoDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}