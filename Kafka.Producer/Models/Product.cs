namespace Kafka.Producer.Models;

public class Product
{
    public required string ProductId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required decimal UnitPrice { get; init; }
    public int AvailableStock { get; set; }
    
    public string GenerateSku()
    {
        return $"SKU-{Name.Replace(" ", "").ToUpper()}-{ProductId.Split('-')[1]}";
    }
}

