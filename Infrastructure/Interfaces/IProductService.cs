using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IProductService // lägg in cancellationToken
{
    void Cancel();
    Task<ProductServiceResult<IEnumerable<Product>>> GetProductsAsync(); // GetProductById(string id);//Name ??
    Task<ProductServiceResult<Product>> SaveProductAsync(Product product);
    Task<ProductServiceResult> UpdateProductAsync(ProductUpdateRequest productUpdateRequest);
    Task<ProductServiceResult> DeleteProductAsync(string id);
}
