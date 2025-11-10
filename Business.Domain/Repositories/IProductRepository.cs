using Business.Domain.Entities;

namespace Business.Domain.Repositories;

public interface IProductRepository
{
    bool TryReserveProducts(List<(Product Product, int Quantity)> items);
    Product? GetRandomProduct(Random random);
    List<Product> GetAllProducts();
    int GetTotalAvailableStock();
    List<Product> GetProductsWithStock(int? stockLimit = null, string? category = null);
}