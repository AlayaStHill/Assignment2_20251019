using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IProductService
{
    void Cancel();
    Task<ProductServiceResult<IEnumerable<Product>>> GetProductsAsync(); // GetProductById(string id); ??
    Task<ProductServiceResult<Product>> SaveProductAsync(Product product);
    Task<ProductServiceResult> UpdateProductAsync(Product updatedProduct);
    Task<ProductServiceResult> DeleteProductAsync(string id);
}
