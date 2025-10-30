using Kafka.Producer.Models;

namespace Kafka.Producer.Services;

public class ProductRepository
{
    private readonly List<Product> _products;
    private readonly object _lock = new();

    public ProductRepository()
    {
        _products = new List<Product>
        {
            new Product 
            { 
                ProductId = "PROD-1001", 
                Name = "Wireless Headphones", 
                Category = "Audio", 
                UnitPrice = 899.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1002", 
                Name = "Gaming Keyboard", 
                Category = "Gaming", 
                UnitPrice = 1299.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1003", 
                Name = "USB-C Cable", 
                Category = "Accessories", 
                UnitPrice = 149.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1004", 
                Name = "Smartphone Stand", 
                Category = "Accessories", 
                UnitPrice = 199.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1005", 
                Name = "Bluetooth Speaker", 
                Category = "Audio", 
                UnitPrice = 699.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1006", 
                Name = "Laptop Charger", 
                Category = "Computing", 
                UnitPrice = 499.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1007", 
                Name = "Wireless Mouse", 
                Category = "Computing", 
                UnitPrice = 349.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1008", 
                Name = "Phone Case", 
                Category = "Mobile", 
                UnitPrice = 249.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1009", 
                Name = "Power Bank", 
                Category = "Mobile", 
                UnitPrice = 399.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1010", 
                Name = "Screen Protector", 
                Category = "Mobile", 
                UnitPrice = 99.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1011", 
                Name = "Memory Card", 
                Category = "Electronics", 
                UnitPrice = 299.99m, 
                AvailableStock = 6 
            },
            new Product 
            { 
                ProductId = "PROD-1012", 
                Name = "Gaming Controller", 
                Category = "Gaming", 
                UnitPrice = 549.99m, 
                AvailableStock = 6 
            }
        };
    }

    public bool TryReserveProducts(List<(Product Product, int Quantity)> items)
    {
        lock (_lock)
        {
            // First check if all items have sufficient stock
            foreach (var (product, quantity) in items)
            {
                var repoProduct = _products.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (repoProduct == null || repoProduct.AvailableStock < quantity)
                {
                    return false;
                }
            }

            // If all items are available, reduce the stock
            foreach (var (product, quantity) in items)
            {
                var repoProduct = _products.First(p => p.ProductId == product.ProductId);
                repoProduct.AvailableStock -= quantity;
            }

            return true;
        }
    }
    
    public Product? GetRandomProduct(Random random)
    {
        lock (_lock)
        {
            var availableProducts = _products.Where(p => p.AvailableStock > 0).ToList();
            if (availableProducts.Count == 0)
            {
                return null;
            }
            return availableProducts[random.Next(availableProducts.Count)];
        }
    }
    
    public List<Product> GetAllProducts()
    {
        lock (_lock)
        {
            return _products.ToList();
        }
    }
    
    public int GetTotalAvailableStock()
    {
        lock (_lock)
        {
            return _products.Sum(p => p.AvailableStock);
        }
    }
}

