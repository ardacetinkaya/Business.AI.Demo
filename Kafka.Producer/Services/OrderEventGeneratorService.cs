using Business.Domain.Events;
using Business.Domain.Repositories;
using Kafka.Producer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kafka.Producer.Services;

public class OrderEventGeneratorService(
    ILogger<OrderEventGeneratorService> logger,
    IProducerService kafkaProducer,
    IOptions<KafkaSettings> kafkaSettings,
    IProductRepository productRepository)
    : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;
    private readonly Random _random = new();


    private readonly string[] _paymentMethods = {
        "CreditCard", "PayPal", "ApplePay", "GooglePay", "BankTransfer", "DebitCard", "Swish"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Order Event Generator Service starting...");
        logger.LogInformation("Initial total stock: {TotalStock} items", productRepository.GetTotalAvailableStock());

        // Wait 5 seconds before starting
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if we have any stock left
                var totalStock = productRepository.GetTotalAvailableStock();
                if (totalStock == 0)
                {
                    logger.LogWarning("No products available in stock. Stopping order generation.");
                    break;
                }

                var orderEvent = GenerateOrderEvent();
                
                // If we couldn't generate an order (insufficient stock), wait and retry
                if (orderEvent == null)
                {
                    logger.LogWarning("Could not generate order due to insufficient stock. Remaining stock: {RemainingStock}", totalStock);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var orderKey = orderEvent.OrderId;

                await kafkaProducer.PublishEventAsync(
                    _kafkaSettings.Topics.OrderEvents,
                    orderKey,
                    orderEvent,
                    stoppingToken);

                logger.LogInformation("Generated and published order event for Order ID: {OrderId}, Remaining stock: {RemainingStock}", 
                    orderEvent.OrderId, productRepository.GetTotalAvailableStock());

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

        logger.LogInformation("Order Event Generator Service stopped. Final stock: {FinalStock}", productRepository.GetTotalAvailableStock());
    }

    private OrderSubmittedEvent? GenerateOrderEvent()
    {
        var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks}";
        var customerId = $"CUST-{_random.Next(100, 999)}";
        var eventId = Guid.NewGuid().ToString();

        var itemCount = _random.Next(1, 4); // 1-3 items per order
        var itemsToReserve = new List<(Business.Domain.Entities.Product Product, int Quantity)>();
        var orderItems = new List<OrderItem>();

        // Try to build the order items
        for (int i = 0; i < itemCount; i++)
        {
            var product = productRepository.GetRandomProduct(_random);
            if (product == null)
            {
                // No products available
                return null;
            }

            // Request 1-2 quantity per item
            var quantity = _random.Next(1, 3);
            
            // Check if we already have this product in the order
            var existingItem = itemsToReserve.FirstOrDefault(x => x.Product.ProductId == product.ProductId);
            if (existingItem.Product != null)
            {
                // Update quantity for existing product
                var index = itemsToReserve.IndexOf(existingItem);
                itemsToReserve[index] = (existingItem.Product, existingItem.Quantity + quantity);
            }
            else
            {
                itemsToReserve.Add((product, quantity));
            }
        }

        // Try to reserve all items atomically
        if (!productRepository.TryReserveProducts(itemsToReserve))
        {
            // Could not reserve all items - insufficient stock
            return null;
        }

        // Build the order items from reserved products
        foreach (var (product, quantity) in itemsToReserve)
        {
            orderItems.Add(new OrderItem
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Sku = product.GenerateSku(),
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TotalPrice = Math.Round(product.UnitPrice * quantity, 2),
                Category = product.Category
            });
        }

        var totalAmount = orderItems.Sum(i => i.TotalPrice);
        var paymentMethod = _paymentMethods[_random.Next(_paymentMethods.Length)];

        return new OrderSubmittedEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            CustomerEmail = $"customer.{customerId.ToLower()}@example.se",
            OrderDate = DateTime.UtcNow.AddMinutes(-_random.Next(0, 60)),
            TotalAmount = Math.Round(totalAmount, 2),
            Currency = "SEK",
            Items = orderItems,
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
                "CreditCard" => "Stripe",
                "DebitCard" => GenerateBank(),
                "Swish" => "Nordea",
                "PayPal" => "PayPal",
                "ApplePay" => "Apple",
                "GooglePay" => "Google",
                "BankTransfer" => GenerateBank(),
                _ => "Unknown"
            },
            TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ProcessedAt = DateTime.UtcNow,
            Status = "Completed"
        };
    }

    private string GenerateBank()
    {
        var list = new List<string> { "Nordea", "SwedBank", "Barclay", "HSBC", "CitiBank", "SEB" };
        var random = new Random();

        return list[random.Next(0, list.Count)];
    }
}