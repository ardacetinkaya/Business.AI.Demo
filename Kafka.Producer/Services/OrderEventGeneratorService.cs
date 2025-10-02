using Kafka.Producer.Configuration;
using Kafka.Producer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kafka.Producer.Services;

public class OrderEventGeneratorService(
    ILogger<OrderEventGeneratorService> logger,
    IProducerService kafkaProducer,
    IOptions<KafkaSettings> kafkaSettings)
    : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;
    private readonly Random _random = new();
    
    private readonly string[] _productNames = {
        "Wireless Headphones", "Gaming Keyboard", "USB-C Cable", "Smartphone Stand",
        "Bluetooth Speaker", "Laptop Charger", "Wireless Mouse", "Phone Case",
        "Power Bank", "Screen Protector", "Memory Card", "Gaming Controller"
    };
    
    private readonly string[] _categories = {
        "Electronics", "Gaming", "Accessories", "Mobile", "Audio", "Computing"
    };
    
    private readonly string[] _paymentMethods = {
        "Credit Card", "PayPal", "Apple Pay", "Google Pay", "Bank Transfer"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Order Event Generator Service starting...");

        // Wait 5 seconds before starting
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var orderEvent = GenerateOrderEvent();
                var orderKey = orderEvent.OrderId;

                await kafkaProducer.PublishEventAsync(
                    _kafkaSettings.Topics.OrderEvents, 
                    orderKey, 
                    orderEvent, 
                    stoppingToken);

                logger.LogInformation("Generated and published order event for Order ID: {OrderId}", orderEvent.OrderId);

                // Generate an event every 5-10 seconds
                var delaySeconds = _random.Next(5, 11);
                logger.LogDebug("Waiting {DelaySeconds} seconds before generating next event", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating or publishing order event");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        logger.LogInformation("Order Event Generator Service stopped");
    }

    private OrderSubmittedEvent GenerateOrderEvent()
    {
        var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks}";
        var customerId = $"CUST-{_random.Next(100, 999)}";
        var eventId = Guid.NewGuid().ToString();

        var itemCount = _random.Next(1, 4); // 1-3 items per order
        var items = new List<OrderItem>();
        
        for (int i = 0; i < itemCount; i++)
        {
            var productName = _productNames[_random.Next(_productNames.Length)];
            var category = _categories[_random.Next(_categories.Length)];
            var quantity = _random.Next(1, 3);
            var unitPrice = _random.Next(10, 200) + (decimal)(_random.NextDouble() * 0.99);
            
            items.Add(new OrderItem
            {
                ProductId = $"PROD-{_random.Next(1000, 9999)}",
                ProductName = productName,
                Sku = $"SKU-{productName.Replace(" ", "").ToUpper()}-{_random.Next(100, 999)}",
                Quantity = quantity,
                UnitPrice = Math.Round(unitPrice, 2),
                TotalPrice = Math.Round(unitPrice * quantity, 2),
                Category = category
            });
        }

        var totalAmount = items.Sum(i => i.TotalPrice);
        var paymentMethod = _paymentMethods[_random.Next(_paymentMethods.Length)];

        return new OrderSubmittedEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            CustomerEmail = $"customer.{customerId.ToLower()}@exempel.se",
            OrderDate = DateTime.UtcNow.AddMinutes(-_random.Next(0, 60)),
            TotalAmount = Math.Round(totalAmount, 2),
            Currency = "SEK",
            Items = items,
            ShippingAddress = GenerateShippingAddress(),
            Payment = GeneratePaymentInfo(paymentMethod),
            Status = "Submitted",
            EventTimestamp = DateTime.UtcNow,
            EventId = eventId
        };
    }

    private ShippingAddress GenerateShippingAddress()
    {
        //Some random mock data
        var firstNames = new[] { "Anders", "Anna", "Erik", "Emma", "Johan", "Maria", "Lars", "Sara", "Nils", "Astrid", "Gustaf", "Ingrid" };
        var lastNames = new[] { "Andersson", "Johansson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson", "Svensson", "Gustafsson" };
        var cities = new[] { "Stockholm", "Göteborg", "Malmö", "Uppsala", "Västerås", "Örebro", "Linköping", "Helsingborg", "Jönköping", "Norrköping" };
        var streetNames = new[] { "Drottninggatan", "Storgatan", "Kungsgatan", "Biblioteksgatan", "Hamngatan", "Vasagatan", "Sveavägen", "Östermalmsgatan" };

        return new ShippingAddress
        {
            FirstName = firstNames[_random.Next(firstNames.Length)],
            LastName = lastNames[_random.Next(lastNames.Length)],
            Street = $"{streetNames[_random.Next(streetNames.Length)]} {_random.Next(1, 150)}",
            City = cities[_random.Next(cities.Length)],
            PostalCode = $"{_random.Next(100, 999)} {_random.Next(10, 99)}",
            Country = "SE"
        };
    }

    private PaymentInfo GeneratePaymentInfo(string paymentMethod)
    {
        return new PaymentInfo
        {
            PaymentMethod = paymentMethod,
            PaymentProvider = paymentMethod switch
            {
                "Credit Card" => "Stripe",
                "PayPal" => "PayPal",
                "Apple Pay" => "Apple",
                "Google Pay" => "Google",
                "Bank Transfer" => "Swedbank",
                _ => "Unknown"
            },
            TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ProcessedAt = DateTime.UtcNow,
            Status = "Completed"
        };
    }
}