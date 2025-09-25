using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IProductService
{
    void Cancel();
    Task<IEnumerable<Product>> GetProductsAsync(); // GetProductById(string id); ??
    Task SaveProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(string id);
}
